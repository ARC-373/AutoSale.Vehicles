using AutoSale.Domain.Vehicles;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Domain.Reservations;

public sealed class VehicleReservation
{
    private VehicleReservation()
    {
    }

    private VehicleReservation(Guid saleId, Vehicle vehicle, DateTimeOffset nowUtc)
    {
        SaleId = saleId;
        VehicleId = vehicle.Id;
        Status = ReservationStatus.Reserved;
        MakeSnapshot = vehicle.Make;
        ModelSnapshot = vehicle.Model;
        YearSnapshot = vehicle.Year;
        ColorSnapshot = vehicle.Color;
        PriceSnapshot = vehicle.Price;
        VehicleVersionSnapshot = vehicle.Version;
        CreatedAtUtc = nowUtc;
    }

    public Guid SaleId { get; private set; }

    public Guid VehicleId { get; private set; }

    public ReservationStatus Status { get; private set; }

    public string MakeSnapshot { get; private set; } = string.Empty;

    public string ModelSnapshot { get; private set; } = string.Empty;

    public int YearSnapshot { get; private set; }

    public string ColorSnapshot { get; private set; } = string.Empty;

    public decimal PriceSnapshot { get; private set; }

    public int VehicleVersionSnapshot { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ConfirmedAtUtc { get; private set; }

    public DateTimeOffset? ReleasedAtUtc { get; private set; }

    public static Result<VehicleReservation> Create(Guid saleId, Vehicle vehicle, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        if (saleId == Guid.Empty)
        {
            return Result.Failure<VehicleReservation>(ReservationErrors.InvalidSaleId);
        }

        if (vehicle.Status != VehicleStatus.Reserved || vehicle.ReservationSaleId != saleId)
        {
            return Result.Failure<VehicleReservation>(ReservationErrors.InvalidVehicle);
        }

        return Result.Success(new VehicleReservation(saleId, vehicle, now.ToUniversalTime()));
    }

    public Result Confirm(DateTimeOffset now)
    {
        if (Status == ReservationStatus.Confirmed)
        {
            return Result.Success();
        }

        if (Status == ReservationStatus.Released)
        {
            return Result.Failure(ReservationErrors.AlreadyReleased);
        }

        Status = ReservationStatus.Confirmed;
        ConfirmedAtUtc = now.ToUniversalTime();
        return Result.Success();
    }

    public Result Release(DateTimeOffset now)
    {
        if (Status == ReservationStatus.Released)
        {
            return Result.Success();
        }

        if (Status == ReservationStatus.Confirmed)
        {
            return Result.Failure(ReservationErrors.AlreadyConfirmed);
        }

        Status = ReservationStatus.Released;
        ReleasedAtUtc = now.ToUniversalTime();
        return Result.Success();
    }
}
