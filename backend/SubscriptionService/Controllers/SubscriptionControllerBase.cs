using Microsoft.AspNetCore.Mvc;
using PRC.Common;

namespace PRC.SubscriptionService.Controllers;

[ApiController]
public abstract class SubscriptionControllerBase : ControllerBase
{
    protected string CorrelationId =>
        HttpContext.Items["CorrelationId"]?.ToString() ?? HttpContext.TraceIdentifier;

    protected IActionResult FromResult<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(ApiResponse<T>.Ok(result.Value!));
        return result.ErrorCode switch
        {
            "NOT_FOUND"        => Problem(detail: result.Error, statusCode: 404, extensions: Ext(result.ErrorCode)),
            "FORBIDDEN"        => Forbid(),
            "CONFLICT"         => Problem(detail: result.Error, statusCode: 409, extensions: Ext(result.ErrorCode)),
            "VALIDATION_ERROR" => Problem(detail: result.Error, statusCode: 400, extensions: Ext(result.ErrorCode)),
            _                  => Problem(detail: result.Error, statusCode: 400, extensions: Ext(result.ErrorCode))
        };
    }

    protected IActionResult FromResult(Result result)
    {
        if (result.IsSuccess) return Ok(ApiResponse<object?>.Ok(null));
        return result.ErrorCode switch
        {
            "NOT_FOUND" => Problem(detail: result.Error, statusCode: 404, extensions: Ext(result.ErrorCode)),
            "FORBIDDEN" => Forbid(),
            "CONFLICT"  => Problem(detail: result.Error, statusCode: 409, extensions: Ext(result.ErrorCode)),
            _           => Problem(detail: result.Error, statusCode: 400, extensions: Ext(result.ErrorCode))
        };
    }

    private static Dictionary<string, object?> Ext(string code) => new() { ["errorCode"] = code };
}
