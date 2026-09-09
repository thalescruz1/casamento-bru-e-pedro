using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using Casamento.Application.Received.Dtos;
using Casamento.Domain.Contributions;
using Casamento.Domain.Gifts;
using MediatR;

namespace Casamento.Application.Received.Queries.ListReceived;

public sealed record ListReceivedQuery : IRequest<Result<IReadOnlyList<ReceivedItemDto>>>;

public sealed class ListReceivedHandler(
    IGiftRepository giftRepository,
    IContributionRepository contributionRepository)
    : IRequestHandler<ListReceivedQuery, Result<IReadOnlyList<ReceivedItemDto>>>
{
    public async Task<Result<IReadOnlyList<ReceivedItemDto>>> Handle(
        ListReceivedQuery request,
        CancellationToken cancellationToken)
    {
        var giftsTask = giftRepository.ListAsync(includeUnavailable: true, cancellationToken);
        var contributionsTask = contributionRepository.ListAsync(cancellationToken);

        await Task.WhenAll(giftsTask, contributionsTask).ConfigureAwait(false);

        var gifts = (await giftsTask.ConfigureAwait(false))
            .Where(g => g.Status == GiftStatus.Paid && g.Purchase is not null)
            .Select(g => new ReceivedItemDto(
                Type: "gift",
                Id: g.Id,
                Title: g.Title,
                Amount: g.Price.Amount,
                Currency: g.Price.Currency,
                ContributorName: g.Purchase!.BuyerName,
                ContributorEmail: g.Purchase!.BuyerEmail.Value,
                Message: g.Purchase!.Message,
                AsaasPaymentId: g.Purchase!.AsaasPaymentId,
                PaymentMethod: ResolvePaymentMethod(g.Purchase!.AsaasPaymentId),
                PaidAt: g.Purchase!.PaidAt));

        var contributions = (await contributionsTask.ConfigureAwait(false))
            .Where(c => c.Status == ContributionStatus.Paid && c.PaidAt.HasValue)
            .Select(c => new ReceivedItemDto(
                Type: "contribution",
                Id: c.Id,
                Title: "Contribuição em valor livre",
                Amount: c.Amount.Amount,
                Currency: c.Amount.Currency,
                ContributorName: c.ContributorName,
                ContributorEmail: c.ContributorEmail?.Value,
                Message: c.Message,
                AsaasPaymentId: c.AsaasPaymentId,
                PaymentMethod: ResolvePaymentMethod(c.AsaasPaymentId),
                PaidAt: c.PaidAt!.Value));

        IReadOnlyList<ReceivedItemDto> merged = gifts
            .Concat(contributions)
            .OrderByDescending(x => x.PaidAt)
            .ToArray();

        return Result<IReadOnlyList<ReceivedItemDto>>.Success(merged);
    }

    // Handlers de Pix manual gravam paymentId com prefixo `manual-pix-` (gifts)
    // ou `manual-contrib-` (contribuições). Cartão vem da Asaas como `pay_*`.
    private static string ResolvePaymentMethod(string? paymentId) =>
        paymentId is not null && paymentId.StartsWith("manual-", StringComparison.Ordinal)
            ? "pix"
            : "card";
}
