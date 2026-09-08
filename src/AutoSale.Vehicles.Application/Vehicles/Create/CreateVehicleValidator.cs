using AutoSale.Domain.Vehicles;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Vehicles.Create;

public static class CreateVehicleValidator
{
    public static Result<Vehicle> Validate(CreateVehicleCommand command, DateTimeOffset now) =>
        Vehicle.Create(command.Make, command.Model, command.Year, command.Color, command.Price, now);
}
