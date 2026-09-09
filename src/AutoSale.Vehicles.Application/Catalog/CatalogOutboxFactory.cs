using System.Text.Json;
using System.Text.Json.Serialization;
using AutoSale.Application.Vehicles;
using AutoSale.Domain.Catalog;
using AutoSale.Domain.Vehicles;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Catalog;

internal static class CatalogOutboxFactory
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static Result<CatalogOutbox> Create(Vehicle vehicle, DateTimeOffset now) =>
        Create(VehicleSnapshotDto.FromDomain(vehicle), now);

    public static Result<CatalogOutbox> Create(VehicleSnapshotDto snapshot, DateTimeOffset now)
    {
        var payloadJson = JsonSerializer.Serialize(snapshot, SerializerOptions);
        return CatalogOutbox.Create(snapshot.Id, snapshot.Version, payloadJson, now);
    }
}
