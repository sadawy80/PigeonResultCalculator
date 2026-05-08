namespace PRC.FederationService.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Role { get; }
    Guid? FederationId { get; }
}
