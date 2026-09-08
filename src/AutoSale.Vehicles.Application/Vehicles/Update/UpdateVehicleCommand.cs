namespace AutoSale.Application.Vehicles.Update;

public sealed record UpdateVehicleCommand(Guid VehicleId, string Make, string Model, int Year, string Color, decimal Price);
