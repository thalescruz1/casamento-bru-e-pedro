using Casamento.Infrastructure.Auth;
using Casamento.Infrastructure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Casamento.Api.Infrastructure;

internal sealed class CosmosBootstrapService(
    CosmosBootstrapper bootstrapper,
    IServiceProvider services,
    ILogger<CosmosBootstrapService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await bootstrapper.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao inicializar Cosmos — seguindo mesmo assim para permitir healthcheck detectar");
            return;
        }

        try
        {
            await using var scope = services.CreateAsyncScope();
            var seed = scope.ServiceProvider.GetRequiredService<AdminSeedService>();
            await seed.SeedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao seedar admin inicial");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
