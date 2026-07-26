using Employee360.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Employee360.Infrastructure.Persistence.Seeding;

/// <summary>
/// Runs all registered <see cref="IDataSeeder"/>s in the background after startup,
/// so an unreachable database never blocks or crashes the host (seeding retries on
/// next start). Disable via "Database:SeedOnStartup": false.
/// </summary>
public sealed class DataSeedHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DataSeedHostedService> _logger;

    public DataSeedHostedService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<DataSeedHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("Database:SeedOnStartup", defaultValue: true))
        {
            _logger.LogInformation("Startup data seeding disabled by configuration");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var seeders = scope.ServiceProvider.GetServices<IDataSeeder>();

        foreach (var seeder in seeders)
        {
            try
            {
                await seeder.SeedAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Data seeding via {Seeder} failed; it will retry on next startup",
                    seeder.GetType().Name);
            }
        }
    }
}
