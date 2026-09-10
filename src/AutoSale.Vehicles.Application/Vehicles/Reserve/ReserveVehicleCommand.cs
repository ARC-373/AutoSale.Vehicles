namespace AutoSale.Application.Vehicles.Reserve;

public sealed record ReserveVehicleCommand(Guid VehicleId, Guid SaleId, decimal ExpectedPrice);
