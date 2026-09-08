namespace AutoSale.Api.Contracts.Vehicles;

public sealed record UpdateVehicleRequest(string Make, string Model, int Year, string Color, decimal Price);
