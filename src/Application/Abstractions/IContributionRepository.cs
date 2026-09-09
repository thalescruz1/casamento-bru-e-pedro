using Casamento.Domain.Contributions;

namespace Casamento.Application.Abstractions;

public interface IContributionRepository
{
    Task AddAsync(Contribution contribution, CancellationToken cancellationToken);

    Task UpdateAsync(Contribution contribution, CancellationToken cancellationToken);

    Task<Contribution?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Contribution?> FindByAsaasPaymentIdAsync(string asaasPaymentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Contribution>> ListAsync(CancellationToken cancellationToken);
}
