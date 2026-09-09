namespace Casamento.Application.Auth.Dtos;

public sealed record LoginResultDto(string Token, DateTimeOffset ExpiresAt, CurrentUserDto User);

public sealed record CurrentUserDto(Guid Id, string Email, string DisplayName);
