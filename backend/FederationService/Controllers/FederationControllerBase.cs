using Microsoft.AspNetCore.Mvc;
using PRC.Common;

namespace PRC.FederationService.Controllers;

[ApiController]
public abstract class FederationControllerBase : ControllerBase
{
    protected IActionResult FromResult<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(ApiResponse<T>.Ok(result.Value!));
        return result.ErrorCode switch
        {
            "NOT_FOUND"        => NotFound(ApiResponse<T>.Fail(result.Error, result.ErrorCode)),
            "FORBIDDEN"        => Forbid(),
            "CONFLICT"         => Conflict(ApiResponse<T>.Fail(result.Error, result.ErrorCode)),
            "VALIDATION_ERROR" => BadRequest(ApiResponse<T>.Fail(result.Error, result.ErrorCode)),
            _                  => BadRequest(ApiResponse<T>.Fail(result.Error, result.ErrorCode))
        };
    }

    protected IActionResult FromResult(Result result)
    {
        if (result.IsSuccess) return Ok(ApiResponse<object?>.Ok(null, "Success"));
        return result.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(ApiResponse<object?>.Fail(result.Error, result.ErrorCode)),
            "FORBIDDEN" => Forbid(),
            "CONFLICT"  => Conflict(ApiResponse<object?>.Fail(result.Error, result.ErrorCode)),
            _           => BadRequest(ApiResponse<object?>.Fail(result.Error, result.ErrorCode))
        };
    }
}
