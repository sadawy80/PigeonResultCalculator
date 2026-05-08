using PRC.IdentityService.DTOs;
using PRC.IdentityService.Models;

namespace PRC.IdentityService.Services;

public interface ITokenService
{
    Task<AuthTokenDto> GenerateTokensAsync(ApplicationUser user, CancellationToken ct = default);
}
