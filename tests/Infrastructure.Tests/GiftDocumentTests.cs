using Casamento.Domain.Gifts;
using Casamento.Domain.Gifts.ValueObjects;
using Casamento.Domain.Rsvps.ValueObjects;
using Casamento.Infrastructure.Cosmos.Documents;
using Newtonsoft.Json;

namespace Casamento.Infrastructure.Tests;

public sealed class GiftDocumentTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Legacy_paid_document_becomes_a_single_purchase_gift()
    {
        const string json = """
        {
          "id": "0192f0c0a1b27000800000000000abcd",
          "type": "gift",
          "title": "Jantar",
          "description": "Desc",
          "priceAmount": 300.0,
          "priceCurrency": "BRL",
          "status": 3,
          "buyerName": "Fulano",
          "buyerEmail": "f@x.com",
          "buyerMessage": "Parabéns!",
          "asaasPaymentId": "pay_1",
          "paidAt": "2026-04-01T12:00:00+00:00",
          "createdAt": "2026-03-01T12:00:00+00:00",
          "updatedAt": "2026-04-01T12:00:00+00:00"
        }
        """;

        var gift = JsonConvert.DeserializeObject<GiftDocument>(json)!.ToAggregate();

        gift.MaxPurchases.Should().Be(1);
        gift.Status.Should().Be(GiftStatus.Paid);
        gift.Purchases.Should().ContainSingle();
        gift.Purchase!.BuyerName.Should().Be("Fulano");
        gift.Purchase.Message.Should().Be("Parabéns!");
        gift.HasPayment("pay_1").Should().BeTrue();
    }

    [Fact]
    public void Legacy_available_document_keeps_a_single_purchase_limit()
    {
        const string json = """
        {
          "id": "0192f0c0a1b27000800000000000abcd",
          "type": "gift",
          "title": "Jantar",
          "description": "Desc",
          "priceAmount": 300.0,
          "priceCurrency": "BRL",
          "status": 1,
          "createdAt": "2026-03-01T12:00:00+00:00",
          "updatedAt": "2026-03-01T12:00:00+00:00"
        }
        """;

        var gift = JsonConvert.DeserializeObject<GiftDocument>(json)!.ToAggregate();

        gift.MaxPurchases.Should().Be(1);
        gift.Status.Should().Be(GiftStatus.Available);
        gift.PurchaseCount.Should().Be(0);
    }

    [Fact]
    public void Unlimited_gift_with_several_purchases_survives_a_round_trip()
    {
        var gift = Gift.Create("Item", "Desc", Money.FromBrl(100m), null, Now, maxPurchases: null);
        gift.MarkPaid("Ana", Email.Create("a@x.com"), "pay_1", Now, "Oi");
        gift.MarkPaid("Bia", Email.Create("b@x.com"), "pay_2", Now.AddMinutes(1));

        var json = JsonConvert.SerializeObject(GiftDocument.FromAggregate(gift));
        var document = JsonConvert.DeserializeObject<GiftDocument>(json)!;
        var restored = document.ToAggregate();

        document.BuyerName.Should().BeNull();
        document.AsaasPaymentId.Should().BeNull();
        restored.MaxPurchases.Should().BeNull();
        restored.Status.Should().Be(GiftStatus.Available);
        restored.Purchases.Select(p => p.AsaasPaymentId).Should().Equal("pay_1", "pay_2");
        restored.Purchases[0].Message.Should().Be("Oi");
    }

    [Fact]
    public void Limited_gift_round_trip_keeps_the_limit()
    {
        var gift = Gift.Create("Item", "Desc", Money.FromBrl(100m), null, Now, maxPurchases: 3);
        gift.MarkPaid("Ana", Email.Create("a@x.com"), "pay_1", Now);

        var json = JsonConvert.SerializeObject(GiftDocument.FromAggregate(gift));
        var restored = JsonConvert.DeserializeObject<GiftDocument>(json)!.ToAggregate();

        restored.MaxPurchases.Should().Be(3);
        restored.PurchaseCount.Should().Be(1);
        restored.Status.Should().Be(GiftStatus.Available);
    }
}
