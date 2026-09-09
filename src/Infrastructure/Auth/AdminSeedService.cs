using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using Casamento.Domain.Auth;
using Casamento.Domain.Rsvps.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Casamento.Infrastructure.Auth;

public sealed class AdminSeedService(
    IAdminUserRepository repository,
    IPasswordHasher hasher,
    IClock clock,
    IOptions<AuthOptions> options,
    ILogger<AdminSeedService> logger)
{
    private readonly AuthOptions _options = options.Value;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await repository.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.SeedEmail) || string.IsNullOrWhiteSpace(_options.SeedPassword))
        {
            logger.LogWarning("Nenhum admin cadastrado e Auth:SeedEmail/Auth:SeedPassword não configurados — painel ficará inacessível até o seed ser preenchido.");
            return;
        }

        var email = Email.Create(_options.SeedEmail);
        var user = AdminUser.Create(
            email,
            string.IsNullOrWhiteSpace(_options.SeedDisplayName) ? "Admin" : _options.SeedDisplayName!,
            hasher.Hash(_options.SeedPassword),
            clock.UtcNow);

        await repository.AddAsync(user, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Admin inicial {Email} criado a partir do seed", email.Value);
    }
}
