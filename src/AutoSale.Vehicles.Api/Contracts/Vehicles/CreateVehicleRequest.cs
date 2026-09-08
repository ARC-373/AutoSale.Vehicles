namespace AutoSale.Api.Contracts.Vehicles;

public sealed record CreateVehicleRequest(string Make, string Model, int Year, string Color, decimal Price);
