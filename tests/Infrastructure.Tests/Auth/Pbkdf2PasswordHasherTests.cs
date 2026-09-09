using Casamento.Infrastructure.Auth;

namespace Casamento.Infrastructure.Tests.Auth;

public sealed class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_and_verify_roundtrip()
    {
        var hashed = _hasher.Hash("SenhaSegura#2026");

        _hasher.Verify(hashed, "SenhaSegura#2026").Should().BeTrue();
        _hasher.Verify(hashed, "errado").Should().BeFalse();
    }

    [Fact]
    public void Each_hash_has_unique_salt()
    {
        var first = _hasher.Hash("senha");
        var second = _hasher.Hash("senha");
        first.Encoded.Should().NotBe(second.Encoded);
        _hasher.Verify(first, "senha").Should().BeTrue();
        _hasher.Verify(second, "senha").Should().BeTrue();
    }
}
