namespace AutoSale.Application.Vehicles.Create;

public sealed record CreateVehicleCommand(string Make, string Model, int Year, string Color, decimal Price);
