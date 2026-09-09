using Casamento.Application.Common;

namespace Casamento.Application.Tests;

internal sealed class TestClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
}
