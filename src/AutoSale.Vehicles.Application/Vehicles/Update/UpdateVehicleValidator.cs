using AutoSale.Application.Common;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Vehicles.Update;

public static class UpdateVehicleValidator
{
    public static Result Validate(UpdateVehicleCommand command)
    {
        if (command.VehicleId == Guid.Empty)
        {
            return Result.Failure(ApplicationErrors.InvalidVehicleId);
        }

        if (command.Version <= 0)
        {
            return Result.Failure(ApplicationErrors.InvalidVehicleVersion);
        }

        return Result.Success();
    }
}
