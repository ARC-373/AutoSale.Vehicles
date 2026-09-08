using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Common;
using AutoSale.Application.Vehicles;
using AutoSale.Application.Vehicles.Create;
using AutoSale.Application.Vehicles.ListAvailable;
using AutoSale.Application.Vehicles.Update;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Api.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationHandlers(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateVehicleCommand, Result<VehicleDto>>, CreateVehicleHandler>();
        services.AddScoped<ICommandHandler<UpdateVehicleCommand, Result<VehicleDto>>, UpdateVehicleHandler>();
        services.AddScoped<IQueryHandler<ListAvailableVehiclesQuery, Result<PagedResult<VehicleDto>>>, ListAvailableVehiclesHandler>();

        return services;
    }
}
