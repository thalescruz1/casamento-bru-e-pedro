using Casamento.Domain.Payments;

namespace Casamento.Application.Abstractions;

public interface IPaymentEventRepository
{
    Task<bool> TryRecordAsync(PaymentEvent evt, CancellationToken cancellationToken);
}
