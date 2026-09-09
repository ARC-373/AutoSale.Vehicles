using System.Data;
using AutoSale.Application.Abstractions.Clock;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Catalog;
using AutoSale.Application.Common;
using AutoSale.Application.Reservations;
using AutoSale.Application.Vehicles;
using AutoSale.Domain.Reservations;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Vehicles.Reserve;

public sealed class ReserveVehicleHandler : ICommandHandler<ReserveVehicleCommand, Result<ReservationResponseDto>>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly ICatalogOutboxRepository _catalogOutboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReserveVehicleHandler(
        IVehicleRepository vehicleRepository,
        IReservationRepository reservationRepository,
        ICatalogOutboxRepository catalogOutboxRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _vehicleRepository = vehicleRepository;
        _reservationRepository = reservationRepository;
        _catalogOutboxRepository = catalogOutboxRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<ReservationResponseDto>> HandleAsync(
        ReserveVehicleCommand command,
        CancellationToken cancellationToken)
    {
        var validation = ReservationCommandValidator.ValidateIds(command.VehicleId, command.SaleId);
        if (validation.IsFailure)
        {
            return Result.Failure<ReservationResponseDto>(validation.Error);
        }

        if (command.ExpectedPrice <= 0 || decimal.Round(command.ExpectedPrice, 2) != command.ExpectedPrice)
        {
            return Result.Failure<ReservationResponseDto>(ApplicationErrors.InvalidExpectedPrice);
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var existing = await _reservationRepository.GetBySaleIdForUpdateAsync(command.SaleId, cancellationToken);
        if (existing is not null)
        {
            return await HandleExistingAsync(existing, command, cancellationToken);
        }

        var vehicle = await _vehicleRepository.GetByIdForUpdateAsync(command.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return Result.Failure<ReservationResponseDto>(ApplicationErrors.VehicleNotFound);
        }

        existing = await _reservationRepository.GetBySaleIdAsync(command.SaleId, cancellationToken);
        if (existing is not null)
        {
            return await HandleExistingAsync(existing, command, cancellationToken);
        }

        var now = _clock.UtcNow;
        var reserve = vehicle.Reserve(command.SaleId, command.ExpectedPrice, now);
        if (reserve.IsFailure)
        {
            return Result.Failure<ReservationResponseDto>(reserve.Error);
        }

        var reservation = VehicleReservation.Create(command.SaleId, vehicle, now);
        if (reservation.IsFailure)
        {
            return Result.Failure<ReservationResponseDto>(reservation.Error);
        }

        var outbox = CatalogOutboxFactory.Create(vehicle, now);
        if (outbox.IsFailure)
        {
            return Result.Failure<ReservationResponseDto>(outbox.Error);
        }

        await _reservationRepository.AddAsync(reservation.Value!, cancellationToken);
        await _catalogOutboxRepository.AddAsync(outbox.Value!, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new ReservationResponseDto(
            command.SaleId,
            ReservationStatus.Reserved,
            VehicleSnapshotDto.FromDomain(vehicle),
            true));
    }

    private async Task<Result<ReservationResponseDto>> HandleExistingAsync(
        VehicleReservation reservation,
        ReserveVehicleCommand command,
        CancellationToken cancellationToken)
    {
        if (reservation.VehicleId != command.VehicleId || reservation.PriceSnapshot != command.ExpectedPrice)
        {
            return Result.Failure<ReservationResponseDto>(ApplicationErrors.ReservationRequestMismatch);
        }

        if (reservation.Status != ReservationStatus.Reserved)
        {
            return Result.Failure<ReservationResponseDto>(ApplicationErrors.ReservationTerminal);
        }

        var vehicle = await _vehicleRepository.GetByIdAsync(command.VehicleId, cancellationToken);
        return vehicle is null
            ? Result.Failure<ReservationResponseDto>(ApplicationErrors.VehicleNotFound)
            : Result.Success(new ReservationResponseDto(
                reservation.SaleId,
                reservation.Status,
                VehicleSnapshotDto.FromDomain(vehicle),
                false));
    }
}
