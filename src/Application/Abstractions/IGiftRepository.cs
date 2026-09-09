using Casamento.Domain.Gifts;

namespace Casamento.Application.Abstractions;

public interface IGiftRepository
{
    Task AddAsync(Gift gift, CancellationToken cancellationToken);

    Task UpdateAsync(Gift gift, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<Gift?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Gift?> FindByAsaasPaymentIdAsync(string asaasPaymentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Gift>> ListAsync(bool includeUnavailable, CancellationToken cancellationToken);
}
