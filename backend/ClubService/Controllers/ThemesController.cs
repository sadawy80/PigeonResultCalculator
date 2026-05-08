using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.Common;
using PRC.ClubService.DTOs;

namespace PRC.ClubService.Controllers;

[Route("api/themes")]
public class ThemesController : ClubControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetAll()
        => Ok(ApiResponse<List<ThemeDto>>.Ok(BuiltInThemes.All));
}
