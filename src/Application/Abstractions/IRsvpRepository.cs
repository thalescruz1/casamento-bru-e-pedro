using Casamento.Domain.Rsvps;

namespace Casamento.Application.Abstractions;

public interface IRsvpRepository
{
    Task AddAsync(Rsvp rsvp, CancellationToken cancellationToken);

    Task<int> CountSubmissionsFromAsync(string ipHash, DateTimeOffset since, CancellationToken cancellationToken);

    Task<IReadOnlyList<Rsvp>> ListAsync(int skip, int take, CancellationToken cancellationToken);

    Task<int> CountAsync(CancellationToken cancellationToken);
}
