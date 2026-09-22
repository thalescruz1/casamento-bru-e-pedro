using Casamento.Application.Abstractions;
using Casamento.Application.Gifts.Commands.DeleteGift;
using Casamento.Domain.Gifts;
using Casamento.Domain.Gifts.ValueObjects;
using Casamento.Domain.Rsvps.ValueObjects;
using NSubstitute;

namespace Casamento.Application.Tests.Gifts;

public sealed class DeleteGiftHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IGiftRepository _repository = Substitute.For<IGiftRepository>();
    private readonly IGiftImageStorage _storage = Substitute.For<IGiftImageStorage>();
    private readonly TestClock _clock = new() { UtcNow = Now };

    private DeleteGiftHandler CreateHandler() => new(_repository, _storage, _clock);

    [Fact]
    public async Task Gift_without_purchases_is_removed_for_real()
    {
        var gift = Gift.Create("Item", "Desc", Money.FromBrl(100m), "blob-1", Now);
        _repository.GetByIdAsync(gift.Id, Arg.Any<CancellationToken>()).Returns(gift);

        var result = await CreateHandler().Handle(new DeleteGiftCommand(gift.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _storage.Received(1).DeleteAsync("blob-1", Arg.Any<CancellationToken>());
        await _repository.Received(1).DeleteAsync(gift.Id, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Gift>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Gift_with_purchases_is_only_marked_deleted_and_purchases_survive()
    {
        var gift = Gift.Create("Item", "Desc", Money.FromBrl(100m), "blob-1", Now);
        gift.MarkPaid("Fulano", Email.Create("f@x.com"), "pay_1", Now);
        _repository.GetByIdAsync(gift.Id, Arg.Any<CancellationToken>()).Returns(gift);

        var result = await CreateHandler().Handle(new DeleteGiftCommand(gift.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        gift.DeletedAt.Should().Be(Now);
        gift.PurchaseCount.Should().Be(1);
        await _repository.Received(1).UpdateAsync(gift, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_gift_returns_not_found()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Gift?)null);

        var result = await CreateHandler().Handle(new DeleteGiftCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Canceled_gift_without_purchases_cannot_be_hard_deleted()
    {
        var gift = Gift.Create("Item", "Desc", Money.FromBrl(100m), null, Now);
        gift.Cancel(Now);
        _repository.GetByIdAsync(gift.Id, Arg.Any<CancellationToken>()).Returns(gift);

        var result = await CreateHandler().Handle(new DeleteGiftCommand(gift.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
