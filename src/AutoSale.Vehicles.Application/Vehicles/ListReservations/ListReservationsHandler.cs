using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.Application.Reservations;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Vehicles.ListReservations;

public sealed class ListReservationsHandler : IQueryHandler<ListReservationsQuery, Result<PagedResult<ReservationDto>>>
{
    private readonly IReservationRepository _reservationRepository;

    public ListReservationsHandler(IReservationRepository reservationRepository)
    {
        _reservationRepository = reservationRepository;
    }

    public async Task<Result<PagedResult<ReservationDto>>> HandleAsync(
        ListReservationsQuery query,
        CancellationToken cancellationToken)
    {
        var validation = PagingValidator.Validate(query.Page, query.PageSize);
        if (validation.IsFailure)
        {
            return Result.Failure<PagedResult<ReservationDto>>(validation.Error);
        }

        var reservations = await _reservationRepository.ListAsync(query.Page, query.PageSize, cancellationToken);
        return Result.Success(reservations.Map(ReservationDto.FromDomain));
    }
}
