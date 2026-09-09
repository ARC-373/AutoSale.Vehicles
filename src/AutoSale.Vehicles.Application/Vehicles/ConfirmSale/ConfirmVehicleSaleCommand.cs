namespace AutoSale.Application.Vehicles.ConfirmSale;

public sealed record ConfirmVehicleSaleCommand(Guid VehicleId, Guid SaleId);
