namespace BuildingBlocks.Infrastructure.CurrentUser;

public interface ICurrentUser
{
    string? UserId { get; }
}