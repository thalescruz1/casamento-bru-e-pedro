using Casamento.Domain.Auth.ValueObjects;

namespace Casamento.Application.Abstractions;

public interface IPasswordHasher
{
    HashedPassword Hash(string plaintext);
    bool Verify(HashedPassword encoded, string plaintext);
}
