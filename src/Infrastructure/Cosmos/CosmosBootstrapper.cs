using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Casamento.Infrastructure.Cosmos;

public sealed class CosmosBootstrapper(
    CosmosClient client,
    IOptions<CosmosOptions> options,
    ILogger<CosmosBootstrapper> logger)
{
    private readonly CosmosOptions _options = options.Value;

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken)
    {
        if (!_options.AutoCreate)
        {
            return;
        }

        var database = await client.CreateDatabaseIfNotExistsAsync(
            _options.DatabaseName,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        await EnsureContainerAsync(database.Database, _options.RsvpsContainer, "/type", cancellationToken).ConfigureAwait(false);
        await EnsureContainerAsync(database.Database, _options.GiftsContainer, "/type", cancellationToken).ConfigureAwait(false);
        await EnsureContainerAsync(database.Database, _options.PaymentEventsContainer, "/giftId", cancellationToken).ConfigureAwait(false);
        await EnsureContainerAsync(database.Database, _options.AdminsContainer, "/type", cancellationToken).ConfigureAwait(false);
        await EnsureContainerAsync(database.Database, _options.ContributionsContainer, "/type", cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Cosmos pronto em database {Database}", _options.DatabaseName);
    }

    private static async Task EnsureContainerAsync(Database database, string name, string partitionKey, CancellationToken cancellationToken)
    {
        await database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(name, partitionKey),
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
