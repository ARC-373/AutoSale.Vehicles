using System.Data;
using AutoSale.Application.Abstractions.Clock;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Catalog;
using AutoSale.Application.Common;
using AutoSale.Application.Reservations;
using AutoSale.Domain.Reservations;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Vehicles.ConfirmSale;

public sealed class ConfirmVehicleSaleHandler : ICommandHandler<ConfirmVehicleSaleCommand, Result<VehicleSnapshotDto>>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly ICatalogOutboxRepository _catalogOutboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ConfirmVehicleSaleHandler(
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

    public async Task<Result<VehicleSnapshotDto>> HandleAsync(
        ConfirmVehicleSaleCommand command,
        CancellationToken cancellationToken)
    {
        var validation = ReservationCommandValidator.ValidateIds(command.VehicleId, command.SaleId);
        if (validation.IsFailure)
        {
            return Result.Failure<VehicleSnapshotDto>(validation.Error);
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var reservation = await _reservationRepository.GetBySaleIdForUpdateAsync(command.SaleId, cancellationToken);
        if (reservation is null)
        {
            return Result.Failure<VehicleSnapshotDto>(ApplicationErrors.ReservationNotFound);
        }

        if (reservation.VehicleId != command.VehicleId)
        {
            return Result.Failure<VehicleSnapshotDto>(ApplicationErrors.ReservationVehicleMismatch);
        }

        var vehicle = await _vehicleRepository.GetByIdForUpdateAsync(command.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return Result.Failure<VehicleSnapshotDto>(ApplicationErrors.VehicleNotFound);
        }

        if (reservation.Status == ReservationStatus.Confirmed)
        {
            return Result.Success(VehicleSnapshotDto.FromDomain(vehicle));
        }

        if (reservation.Status == ReservationStatus.Released)
        {
            return Result.Failure<VehicleSnapshotDto>(ApplicationErrors.ReservationTerminal);
        }

        var now = _clock.UtcNow;
        var confirmation = vehicle.ConfirmSale(command.SaleId, now);
        if (confirmation.IsFailure)
        {
            return Result.Failure<VehicleSnapshotDto>(confirmation.Error);
        }

        var reservationConfirmation = reservation.Confirm(now);
        if (reservationConfirmation.IsFailure)
        {
            return Result.Failure<VehicleSnapshotDto>(reservationConfirmation.Error);
        }

        var outbox = CatalogOutboxFactory.Create(vehicle, now);
        if (outbox.IsFailure)
        {
            return Result.Failure<VehicleSnapshotDto>(outbox.Error);
        }

        await _catalogOutboxRepository.AddAsync(outbox.Value!, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success(VehicleSnapshotDto.FromDomain(vehicle));
    }
}
