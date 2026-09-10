using System.Data;
using AutoSale.Application.Abstractions.Clock;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Catalog;
using AutoSale.Application.Common;
using AutoSale.Application.Reservations;
using AutoSale.Domain.Reservations;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Vehicles.ReleaseReservation;

public sealed class ReleaseVehicleReservationHandler : ICommandHandler<ReleaseVehicleReservationCommand, Result<VehicleSnapshotDto>>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly ICatalogOutboxRepository _catalogOutboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReleaseVehicleReservationHandler(
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
        ReleaseVehicleReservationCommand command,
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

        if (reservation.Status == ReservationStatus.Released)
        {
            return Result.Success(VehicleSnapshotDto.FromDomain(vehicle));
        }

        if (reservation.Status == ReservationStatus.Confirmed)
        {
            return Result.Failure<VehicleSnapshotDto>(ApplicationErrors.ReservationTerminal);
        }

        var now = _clock.UtcNow;
        var release = vehicle.ReleaseReservation(command.SaleId, now);
        if (release.IsFailure)
        {
            return Result.Failure<VehicleSnapshotDto>(release.Error);
        }

        var reservationRelease = reservation.Release(now);
        if (reservationRelease.IsFailure)
        {
            return Result.Failure<VehicleSnapshotDto>(reservationRelease.Error);
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
