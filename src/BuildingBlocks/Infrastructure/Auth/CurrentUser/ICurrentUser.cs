namespace BuildingBlocks.Infrastructure.Auth.CurrentUser;

public interface ICurrentUser
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
}