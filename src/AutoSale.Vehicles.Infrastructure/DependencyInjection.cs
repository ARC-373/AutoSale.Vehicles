using AutoSale.Application.Abstractions.Clock;
using AutoSale.Application.Abstractions.Integrations;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Infrastructure.BackgroundServices;
using AutoSale.Infrastructure.Clock;
using AutoSale.Infrastructure.Integrations.Sales;
using AutoSale.Infrastructure.Persistence;
using AutoSale.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AutoSale.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<AutoSaleDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AutoSaleDbContext).Assembly.FullName)));

        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<ICatalogOutboxRepository, CatalogOutboxRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IClock, SystemClock>();

        services.Configure<SalesIntegrationOptions>(configuration.GetSection(SalesIntegrationOptions.SectionName));
        services.Configure<CatalogPublisherOptions>(configuration.GetSection(CatalogPublisherOptions.SectionName));
        services.AddHttpClient<ISalesCatalogClient, SalesCatalogClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<SalesIntegrationOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds is > 0 and <= 300 ? options.TimeoutSeconds : 10);
        });
        services.AddHostedService<CatalogPublisherWorker>();

        return services;
    }
}
