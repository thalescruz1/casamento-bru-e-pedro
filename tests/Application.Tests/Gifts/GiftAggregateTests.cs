using Casamento.Domain.Common;
using Casamento.Domain.Gifts;
using Casamento.Domain.Gifts.Events;
using Casamento.Domain.Gifts.ValueObjects;
using Casamento.Domain.Rsvps.ValueObjects;

namespace Casamento.Application.Tests.Gifts;

public sealed class GiftAggregateTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_starts_as_available()
    {
        var gift = Gift.Create("Jantar à luz de velas", "Descrição", Money.FromBrl(500m), null, Now);

        gift.Status.Should().Be(GiftStatus.Available);
        gift.DomainEvents.Should().ContainSingle(e => e is GiftCreated);
    }

    [Fact]
    public void MarkPaid_transitions_available_to_paid()
    {
        var gift = Gift.Create("Título válido", "Desc", Money.FromBrl(300m), null, Now);

        gift.MarkPaid("Fulano", Email.Create("f@x.com"), "pay_1", Now);

        gift.Status.Should().Be(GiftStatus.Paid);
        gift.Purchase.Should().NotBeNull();
        gift.Purchase!.BuyerName.Should().Be("Fulano");
        gift.Purchase.AsaasPaymentId.Should().Be("pay_1");
        gift.DomainEvents.OfType<GiftPaid>().Should().ContainSingle();
    }

    [Fact]
    public void MarkPaid_is_idempotent_for_same_payment_id()
    {
        var gift = Gift.Create("Item", "Desc", Money.FromBrl(100m), null, Now);
        gift.MarkPaid("Fulano", Email.Create("a@x.com"), "pay_1", Now);

        var act = () => gift.MarkPaid("Fulano", Email.Create("a@x.com"), "pay_1", Now);

        act.Should().NotThrow();
        gift.Status.Should().Be(GiftStatus.Paid);
    }

    [Fact]
    public void MarkPaid_second_buyer_emits_conflict_without_changing_state()
    {
        var gift = Gift.Create("Item", "Desc", Money.FromBrl(100m), null, Now);
        gift.MarkPaid("First", Email.Create("first@x.com"), "pay_1", Now);

        gift.MarkPaid("Second", Email.Create("second@x.com"), "pay_2", Now.AddMinutes(5));

        gift.Purchase!.BuyerName.Should().Be("First");
        gift.Purchase.AsaasPaymentId.Should().Be("pay_1");
        gift.DomainEvents.OfType<GiftPaymentConflict>().Should().ContainSingle();
    }

    [Fact]
    public void MarkPaid_rejects_when_canceled()
    {
        var gift = Gift.Create("Item", "Desc", Money.FromBrl(100m), null, Now);
        gift.Cancel(Now);

        var act = () => gift.MarkPaid("X", Email.Create("x@x.com"), "pay_1", Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_cannot_revert_paid()
    {
        var gift = Gift.Create("Título válido", "Desc", Money.FromBrl(300m), null, Now);
        gift.MarkPaid("Guest", Email.Create("g@x.com"), "pay_1", Now);

        var act = () => gift.Cancel(Now);

        act.Should().Throw<DomainException>();
    }
}
