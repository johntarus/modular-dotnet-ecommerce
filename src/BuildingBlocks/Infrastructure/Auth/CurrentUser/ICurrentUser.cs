namespace BuildingBlocks.Infrastructure.CurrentUser;

public interface ICurrentUser
{
    Guid? UserId { get; }
}