using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Catalog.PublishPending;
using AutoSale.Application.Catalog.Rebuild;
using AutoSale.Application.Common;
using AutoSale.Application.Reservations;
using AutoSale.Application.Vehicles;
using AutoSale.Application.Vehicles.ConfirmSale;
using AutoSale.Application.Vehicles.Create;
using AutoSale.Application.Vehicles.GetById;
using AutoSale.Application.Vehicles.List;
using AutoSale.Application.Vehicles.ListReservations;
using AutoSale.Application.Vehicles.ListVehicleReservations;
using AutoSale.Application.Vehicles.ReleaseReservation;
using AutoSale.Application.Vehicles.Reserve;
using AutoSale.Application.Vehicles.Update;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Api.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationHandlers(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateVehicleCommand, Result<VehicleDto>>, CreateVehicleHandler>();
        services.AddScoped<ICommandHandler<UpdateVehicleCommand, Result<VehicleDto>>, UpdateVehicleHandler>();
        services.AddScoped<IQueryHandler<GetVehicleByIdQuery, Result<VehicleDto>>, GetVehicleByIdHandler>();
        services.AddScoped<IQueryHandler<ListVehiclesQuery, Result<PagedResult<VehicleDto>>>, ListVehiclesHandler>();
        services.AddScoped<ICommandHandler<ReserveVehicleCommand, Result<ReservationResponseDto>>, ReserveVehicleHandler>();
        services.AddScoped<ICommandHandler<ConfirmVehicleSaleCommand, Result<VehicleSnapshotDto>>, ConfirmVehicleSaleHandler>();
        services.AddScoped<ICommandHandler<ReleaseVehicleReservationCommand, Result<VehicleSnapshotDto>>, ReleaseVehicleReservationHandler>();
        services.AddScoped<IQueryHandler<ListReservationsQuery, Result<PagedResult<ReservationDto>>>, ListReservationsHandler>();
        services.AddScoped<IQueryHandler<ListVehicleReservationsQuery, Result<PagedResult<ReservationDto>>>, ListVehicleReservationsHandler>();
        services.AddScoped<ICommandHandler<PublishPendingCatalogCommand, Result<CatalogPublishSummary>>, PublishPendingCatalogHandler>();
        services.AddScoped<ICommandHandler<RebuildCatalogCommand, Result<int>>, RebuildCatalogHandler>();

        return services;
    }
}
