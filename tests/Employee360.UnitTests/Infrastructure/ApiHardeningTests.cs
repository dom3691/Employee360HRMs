using Employee360.Infrastructure.HealthChecks;
using Employee360.Infrastructure.Services.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Employee360.UnitTests.Infrastructure;

public sealed class BlobStorageHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenConnectionStringMissing_ReturnsDegraded()
    {
        var settings = Options.Create(new BlobStorageSettings { ConnectionString = "" });
        var check = new BlobStorageHealthCheck(settings);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
    }
}

public sealed class DemoDataSeederTests
{
    [Fact]
    public void DemoDataSeeder_IsRegisteredAsIDataSeeder()
    {
        var type = typeof(Employee360.Infrastructure.Persistence.Seeding.DemoDataSeeder);
        Assert.True(typeof(Employee360.Application.Common.Interfaces.IDataSeeder).IsAssignableFrom(type));
    }
}
