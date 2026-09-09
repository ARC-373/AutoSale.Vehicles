using AutoSale.Application.Common;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Reservations;

internal static class ReservationCommandValidator
{
    public static Result ValidateIds(Guid vehicleId, Guid saleId)
    {
        if (vehicleId == Guid.Empty)
        {
            return Result.Failure(ApplicationErrors.InvalidVehicleId);
        }

        return saleId == Guid.Empty
            ? Result.Failure(ApplicationErrors.InvalidSaleId)
            : Result.Success();
    }
}
