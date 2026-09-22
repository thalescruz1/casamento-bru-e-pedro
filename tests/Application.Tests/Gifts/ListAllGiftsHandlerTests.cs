using Casamento.Application.Abstractions;
using Casamento.Application.Gifts.Queries.ListAllGifts;
using Casamento.Domain.Gifts;
using Casamento.Domain.Gifts.ValueObjects;
using Casamento.Domain.Rsvps.ValueObjects;
using NSubstitute;

namespace Casamento.Application.Tests.Gifts;

public sealed class ListAllGiftsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IGiftRepository _repository = Substitute.For<IGiftRepository>();
    private readonly IGiftImageStorage _storage = Substitute.For<IGiftImageStorage>();

    [Fact]
    public async Task Deleted_gifts_are_hidden_from_the_admin_list_but_others_stay()
    {
        var active = Gift.Create("Ativo", "Desc", Money.FromBrl(100m), null, Now);

        var deleted = Gift.Create("Excluído", "Desc", Money.FromBrl(200m), null, Now);
        deleted.MarkPaid("Fulano", Email.Create("f@x.com"), "pay_1", Now);
        deleted.MarkDeleted(Now);

        _repository.ListAsync(true, Arg.Any<CancellationToken>()).Returns(new[] { active, deleted });

        var handler = new ListAllGiftsHandler(_repository, _storage);
        var result = await handler.Handle(new ListAllGiftsQuery(), CancellationToken.None);

        result.Value!.Select(g => g.Title).Should().BeEquivalentTo("Ativo");
    }
}
