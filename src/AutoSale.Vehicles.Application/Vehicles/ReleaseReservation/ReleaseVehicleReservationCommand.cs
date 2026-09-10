namespace AutoSale.Application.Vehicles.ReleaseReservation;

public sealed record ReleaseVehicleReservationCommand(Guid VehicleId, Guid SaleId);
