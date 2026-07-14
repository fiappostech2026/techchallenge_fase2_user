namespace FCG.Usuario.Domain.Dto;

public sealed class UserCreatedEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
}
