using Casamento.Domain.Common;
using Casamento.Domain.Gifts;
using Casamento.Domain.Gifts.Events;
using Casamento.Domain.Gifts.ValueObjects;
using Casamento.Domain.Rsvps.ValueObjects;

namespace Casamento.Application.Tests.Gifts;

public sealed class GiftMultiplePurchasesTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

    private static Gift NewGift(int? maxPurchases) =>
        Gift.Create("Item", "Desc", Money.FromBrl(100m), null, Now, maxPurchases);

    private static bool Buy(Gift gift, string paymentId) =>
        gift.MarkPaid("Fulano", Email.Create($"{paymentId}@x.com"), paymentId, Now);

    [Fact]
    public void Default_limit_is_a_single_purchase()
    {
        var gift = Gift.Create("Item", "Desc", Money.FromBrl(100m), null, Now);

        gift.MaxPurchases.Should().Be(1);
        Buy(gift, "pay_1");
        gift.Status.Should().Be(GiftStatus.Paid);
    }

    [Fact]
    public void Limited_gift_stays_available_until_the_limit_is_reached()
    {
        var gift = NewGift(3);

        Buy(gift, "pay_1").Should().BeTrue();
        Buy(gift, "pay_2").Should().BeTrue();
        gift.Status.Should().Be(GiftStatus.Available);
        gift.PurchaseCount.Should().Be(2);

        Buy(gift, "pay_3").Should().BeTrue();
        gift.Status.Should().Be(GiftStatus.Paid);
        gift.Purchases.Select(p => p.AsaasPaymentId).Should().Equal("pay_1", "pay_2", "pay_3");
    }

    [Fact]
    public void Purchase_after_the_limit_emits_conflict_without_changing_state()
    {
        var gift = NewGift(2);
        Buy(gift, "pay_1");
        Buy(gift, "pay_2");

        Buy(gift, "pay_3").Should().BeFalse();

        gift.PurchaseCount.Should().Be(2);
        gift.DomainEvents.OfType<GiftPaymentConflict>().Should().ContainSingle();
    }

    [Fact]
    public void Unlimited_gift_never_sells_out()
    {
        var gift = NewGift(null);

        for (var i = 1; i <= 25; i++)
        {
            Buy(gift, $"pay_{i}").Should().BeTrue();
        }

        gift.Status.Should().Be(GiftStatus.Available);
        gift.PurchaseCount.Should().Be(25);
    }

    [Fact]
    public void Same_payment_twice_is_recorded_once()
    {
        var gift = NewGift(5);

        Buy(gift, "pay_1").Should().BeTrue();
        Buy(gift, "pay_1").Should().BeFalse();

        gift.PurchaseCount.Should().Be(1);
        gift.DomainEvents.OfType<GiftPaymentConflict>().Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(Gift.MaxPurchasesLimit + 1)]
    public void Create_rejects_an_invalid_limit(int limit)
    {
        var act = () => NewGift(limit);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Raising_the_limit_reopens_a_sold_out_gift()
    {
        var gift = NewGift(1);
        Buy(gift, "pay_1");
        gift.Status.Should().Be(GiftStatus.Paid);

        gift.UpdateDetails("Item", "Desc", Money.FromBrl(100m), null, 3, Now);

        gift.Status.Should().Be(GiftStatus.Available);
        Buy(gift, "pay_2").Should().BeTrue();
    }

    [Fact]
    public void Making_a_gift_unlimited_reopens_it()
    {
        var gift = NewGift(1);
        Buy(gift, "pay_1");

        gift.UpdateDetails("Item", "Desc", Money.FromBrl(100m), null, null, Now);

        gift.MaxPurchases.Should().BeNull();
        gift.Status.Should().Be(GiftStatus.Available);
    }

    [Fact]
    public void Limit_cannot_go_below_the_purchases_already_made()
    {
        var gift = NewGift(5);
        Buy(gift, "pay_1");
        Buy(gift, "pay_2");

        var act = () => gift.UpdateDetails("Item", "Desc", Money.FromBrl(100m), null, 1, Now);

        act.Should().Throw<DomainException>();
        gift.MaxPurchases.Should().Be(5);
    }

    [Fact]
    public void Lowering_the_limit_to_the_purchases_made_sells_the_gift_out()
    {
        var gift = NewGift(5);
        Buy(gift, "pay_1");
        Buy(gift, "pay_2");

        gift.UpdateDetails("Item", "Desc", Money.FromBrl(100m), null, 2, Now);

        gift.Status.Should().Be(GiftStatus.Paid);
    }

    [Fact]
    public void Price_is_locked_after_the_first_purchase_but_texts_can_change()
    {
        var gift = NewGift(5);
        Buy(gift, "pay_1");

        var changePrice = () => gift.UpdateDetails("Item", "Desc", Money.FromBrl(250m), null, 5, Now);
        changePrice.Should().Throw<DomainException>();

        gift.UpdateDetails("Novo título", "Nova descrição", Money.FromBrl(100m), null, 5, Now);
        gift.Title.Should().Be("Novo título");
    }

    [Fact]
    public void Price_can_change_while_there_are_no_purchases()
    {
        var gift = NewGift(2);

        gift.UpdateDetails("Item", "Desc", Money.FromBrl(250m), null, 2, Now);

        gift.Price.Amount.Should().Be(250m);
    }

    [Fact]
    public void Gift_with_purchases_cannot_be_canceled()
    {
        var gift = NewGift(5);
        Buy(gift, "pay_1");

        var act = () => gift.Cancel(Now);

        act.Should().Throw<DomainException>();
        gift.Status.Should().Be(GiftStatus.Available);
    }

    [Fact]
    public void Canceled_gift_cannot_be_edited()
    {
        var gift = NewGift(1);
        gift.Cancel(Now);

        var act = () => gift.UpdateDetails("Item", "Desc", Money.FromBrl(100m), null, 1, Now);

        act.Should().Throw<DomainException>();
    }
}
