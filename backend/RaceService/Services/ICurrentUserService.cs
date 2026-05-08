namespace PRC.RaceService.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Role { get; }
    Guid? FederationId { get; }
    Guid? ClubId { get; }
}
