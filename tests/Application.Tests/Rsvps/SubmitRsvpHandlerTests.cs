using Casamento.Application.Abstractions;
using Casamento.Application.Rsvps.Commands.SubmitRsvp;
using Casamento.Domain.Rsvps;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Casamento.Application.Tests.Rsvps;

public sealed class SubmitRsvpHandlerTests
{
    private readonly IRsvpRepository _repository = Substitute.For<IRsvpRepository>();
    private readonly TestClock _clock = new();

    private static SubmitRsvpCommand ValidCommand(Attendance attend = Attendance.Sim, int guests = 1, string? guestNames = "Maria da Silva") =>
        new("Thales Cruz", "thales@example.com", "(11) 9 8765-4321", attend, guests, guestNames, null, "hash");

    [Fact]
    public async Task Submits_valid_rsvp_and_persists()
    {
        _repository.CountSubmissionsFromAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var handler = new SubmitRsvpHandler(_repository, _clock, NullLogger<SubmitRsvpHandler>.Instance);
        var command = ValidCommand();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Thales Cruz");
        result.Value.Attend.Should().Be(Attendance.Sim);
        await _repository.Received(1).AddAsync(Arg.Any<Rsvp>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_when_rate_limited()
    {
        _repository.CountSubmissionsFromAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(3);

        var handler = new SubmitRsvpHandler(_repository, _clock, NullLogger<SubmitRsvpHandler>.Instance);

        var result = await handler.Handle(ValidCommand(guests: 0, guestNames: null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("rate_limited");
        await _repository.DidNotReceive().AddAsync(Arg.Any<Rsvp>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_when_attend_nao_with_guest()
    {
        _repository.CountSubmissionsFromAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var handler = new SubmitRsvpHandler(_repository, _clock, NullLogger<SubmitRsvpHandler>.Instance);

        var result = await handler.Handle(ValidCommand(attend: Attendance.Nao, guests: 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
    }

    [Fact]
    public async Task Rejects_invalid_email()
    {
        _repository.CountSubmissionsFromAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var handler = new SubmitRsvpHandler(_repository, _clock, NullLogger<SubmitRsvpHandler>.Instance);
        var command = ValidCommand() with { Email = "invalido" };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
    }

    [Fact]
    public async Task Rejects_when_plus_one_without_guest_names()
    {
        _repository.CountSubmissionsFromAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var handler = new SubmitRsvpHandler(_repository, _clock, NullLogger<SubmitRsvpHandler>.Instance);

        var result = await handler.Handle(ValidCommand(guests: 1, guestNames: null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
    }

    [Fact]
    public async Task Rejects_invalid_phone()
    {
        _repository.CountSubmissionsFromAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var handler = new SubmitRsvpHandler(_repository, _clock, NullLogger<SubmitRsvpHandler>.Instance);
        var command = ValidCommand() with { Phone = "123" };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
    }
}
