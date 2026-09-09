namespace AutoSale.Application.Vehicles.ListVehicleReservations;

public sealed record ListVehicleReservationsQuery(Guid VehicleId, int Page, int PageSize);
