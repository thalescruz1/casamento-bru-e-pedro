using Casamento.Application.Abstractions;
using Casamento.Application.Received.Queries.ListReceived;
using Casamento.Domain.Contributions;
using Casamento.Domain.Gifts;
using Casamento.Domain.Gifts.ValueObjects;
using Casamento.Domain.Rsvps.ValueObjects;
using NSubstitute;

namespace Casamento.Application.Tests.Received;

public sealed class ListReceivedHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IGiftRepository _giftRepository = Substitute.For<IGiftRepository>();
    private readonly IContributionRepository _contributionRepository = Substitute.For<IContributionRepository>();

    [Fact]
    public async Task Deleted_gift_purchases_still_show_up_with_the_amount_actually_paid()
    {
        var gift = Gift.Create("Item", "Desc", Money.FromBrl(100m), null, Now);
        gift.MarkPaid("Ana", Email.Create("a@x.com"), "pay_1", Now);

        // Preço sobe depois da primeira compra; a compra antiga tem que continuar com R$100.
        gift.UpdateDetails("Item", "Desc", Money.FromBrl(250m), null, null, Now.AddHours(1));
        gift.MarkPaid("Bia", Email.Create("b@x.com"), "pay_2", Now.AddHours(2));

        gift.MarkDeleted(Now.AddHours(3));

        _giftRepository.ListAsync(true, Arg.Any<CancellationToken>()).Returns(new[] { gift });
        _contributionRepository.ListAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Contribution>());

        var handler = new ListReceivedHandler(_giftRepository, _contributionRepository);
        var result = await handler.Handle(new ListReceivedQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2, "as duas compras continuam em Recebidos mesmo o presente tendo sido excluído");
        result.Value!.Single(x => x.ContributorName == "Ana").Amount.Should().Be(100m);
        result.Value!.Single(x => x.ContributorName == "Bia").Amount.Should().Be(250m);
    }
}
