using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.Application.Reservations;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Vehicles.ListVehicleReservations;

public sealed class ListVehicleReservationsHandler : IQueryHandler<ListVehicleReservationsQuery, Result<PagedResult<ReservationDto>>>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IReservationRepository _reservationRepository;

    public ListVehicleReservationsHandler(IVehicleRepository vehicleRepository, IReservationRepository reservationRepository)
    {
        _vehicleRepository = vehicleRepository;
        _reservationRepository = reservationRepository;
    }

    public async Task<Result<PagedResult<ReservationDto>>> HandleAsync(
        ListVehicleReservationsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.VehicleId == Guid.Empty)
        {
            return Result.Failure<PagedResult<ReservationDto>>(ApplicationErrors.InvalidVehicleId);
        }

        var validation = PagingValidator.Validate(query.Page, query.PageSize);
        if (validation.IsFailure)
        {
            return Result.Failure<PagedResult<ReservationDto>>(validation.Error);
        }

        if (await _vehicleRepository.GetByIdAsync(query.VehicleId, cancellationToken) is null)
        {
            return Result.Failure<PagedResult<ReservationDto>>(ApplicationErrors.VehicleNotFound);
        }

        var reservations = await _reservationRepository.ListByVehicleIdAsync(
            query.VehicleId,
            query.Page,
            query.PageSize,
            cancellationToken);
        return Result.Success(reservations.Map(ReservationDto.FromDomain));
    }
}
