using Casamento.Domain.Common;
using Casamento.Domain.Contributions;
using Casamento.Domain.Contributions.Events;
using Casamento.Domain.Gifts.ValueObjects;
using Casamento.Domain.Rsvps.ValueObjects;

namespace Casamento.Application.Tests.Contributions;

public sealed class ContributionAggregateTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_starts_as_pending_and_emits_created_event()
    {
        var contribution = Contribution.Create(Money.FromBrl(50m), Now);

        contribution.Status.Should().Be(ContributionStatus.Pending);
        contribution.Amount.Amount.Should().Be(50m);
        contribution.DomainEvents.Should().ContainSingle(e => e is ContributionCreated);
    }

    [Fact]
    public void Create_rejects_amount_below_minimum()
    {
        var act = () => Contribution.Create(Money.FromBrl(4.99m), Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkPaid_transitions_pending_to_paid()
    {
        var contribution = Contribution.Create(Money.FromBrl(100m), Now);

        contribution.MarkPaid("Fulano", Email.Create("f@x.com"), "pay_1", Now, "Boa sorte!");

        contribution.Status.Should().Be(ContributionStatus.Paid);
        contribution.ContributorName.Should().Be("Fulano");
        contribution.ContributorEmail!.Value.Value.Should().Be("f@x.com");
        contribution.Message.Should().Be("Boa sorte!");
        contribution.AsaasPaymentId.Should().Be("pay_1");
        contribution.PaidAt.Should().Be(Now);
        contribution.DomainEvents.OfType<ContributionPaid>().Should().ContainSingle();
    }

    [Fact]
    public void MarkPaid_is_idempotent_for_same_payment_id()
    {
        var contribution = Contribution.Create(Money.FromBrl(100m), Now);
        contribution.MarkPaid("Fulano", Email.Create("a@x.com"), "pay_1", Now);

        var act = () => contribution.MarkPaid("Fulano", Email.Create("a@x.com"), "pay_1", Now);

        act.Should().NotThrow();
        contribution.Status.Should().Be(ContributionStatus.Paid);
    }

    [Fact]
    public void MarkPaid_second_payment_emits_conflict_without_changing_state()
    {
        var contribution = Contribution.Create(Money.FromBrl(100m), Now);
        contribution.MarkPaid("First", Email.Create("first@x.com"), "pay_1", Now);

        contribution.MarkPaid("Second", Email.Create("second@x.com"), "pay_2", Now.AddMinutes(5));

        contribution.ContributorName.Should().Be("First");
        contribution.AsaasPaymentId.Should().Be("pay_1");
        contribution.DomainEvents.OfType<ContributionPaymentConflict>().Should().ContainSingle();
    }

    [Fact]
    public void MarkPaid_rejects_when_canceled()
    {
        var contribution = Contribution.Create(Money.FromBrl(100m), Now);
        contribution.Cancel(Now);

        var act = () => contribution.MarkPaid("X", Email.Create("x@x.com"), "pay_1", Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_cannot_revert_paid()
    {
        var contribution = Contribution.Create(Money.FromBrl(100m), Now);
        contribution.MarkPaid("Guest", Email.Create("g@x.com"), "pay_1", Now);

        var act = () => contribution.Cancel(Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkPaid_rejects_message_above_max_length()
    {
        var contribution = Contribution.Create(Money.FromBrl(100m), Now);
        var longMessage = new string('a', Contribution.MaxMessageLength + 1);

        var act = () => contribution.MarkPaid("X", Email.Create("x@x.com"), "pay_1", Now, longMessage);

        act.Should().Throw<DomainException>();
    }
}
