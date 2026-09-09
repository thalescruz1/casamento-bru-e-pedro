using Casamento.Domain.Auth;

namespace Casamento.Application.Abstractions;

public interface IAdminUserRepository
{
    Task<AdminUser?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<AdminUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> AnyAsync(CancellationToken cancellationToken);
    Task AddAsync(AdminUser user, CancellationToken cancellationToken);
    Task UpdateAsync(AdminUser user, CancellationToken cancellationToken);
}
