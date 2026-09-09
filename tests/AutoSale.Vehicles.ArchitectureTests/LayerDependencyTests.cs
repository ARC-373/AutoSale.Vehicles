using AutoSale.Application.Vehicles;
using AutoSale.Domain.Vehicles;
using AutoSale.Infrastructure.Persistence;

namespace AutoSale.Vehicles.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_ShouldNotReferenceOuterLayers()
    {
        var references = typeof(Vehicle).Assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

        Assert.DoesNotContain("AutoSale.Vehicles.Application", references);
        Assert.DoesNotContain("AutoSale.Vehicles.Infrastructure", references);
        Assert.DoesNotContain("AutoSale.Vehicles.Api", references);
    }

    [Fact]
    public void Application_ShouldNotReferenceInfrastructureOrApi()
    {
        var references = typeof(VehicleDto).Assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

        Assert.DoesNotContain("AutoSale.Vehicles.Infrastructure", references);
        Assert.DoesNotContain("AutoSale.Vehicles.Api", references);
    }

    [Fact]
    public void Infrastructure_ShouldNotReferenceApi()
    {
        var references = typeof(AutoSaleDbContext).Assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

        Assert.DoesNotContain("AutoSale.Vehicles.Api", references);
    }
}
