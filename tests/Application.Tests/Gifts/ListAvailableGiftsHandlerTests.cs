using Casamento.Application.Abstractions;
using Casamento.Application.Gifts.Queries.ListAvailableGifts;
using Casamento.Domain.Gifts;
using Casamento.Domain.Gifts.ValueObjects;
using Casamento.Domain.Rsvps.ValueObjects;
using NSubstitute;

namespace Casamento.Application.Tests.Gifts;

public sealed class ListAvailableGiftsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IGiftRepository _repository = Substitute.For<IGiftRepository>();
    private readonly IGiftImageStorage _storage = Substitute.For<IGiftImageStorage>();

    [Fact]
    public async Task Paid_gifts_stay_in_the_public_list_and_canceled_ones_do_not()
    {
        var available = Gift.Create("Disponível", "Desc", Money.FromBrl(100m), null, Now);

        var paid = Gift.Create("Comprado", "Desc", Money.FromBrl(200m), null, Now);
        paid.MarkPaid("Fulano", Email.Create("f@x.com"), "pay_1", Now);

        var canceled = Gift.Create("Cancelado", "Desc", Money.FromBrl(300m), null, Now);
        canceled.Cancel(Now);

        _repository.ListAsync(true, Arg.Any<CancellationToken>())
            .Returns(new[] { available, paid, canceled });

        var handler = new ListAvailableGiftsHandler(_repository, _storage);
        var result = await handler.Handle(new ListAvailableGiftsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(g => g.Title).Should().BeEquivalentTo("Disponível", "Comprado");
        result.Value!.Single(g => g.Title == "Comprado").Status.Should().Be(GiftStatus.Paid);
    }
}
