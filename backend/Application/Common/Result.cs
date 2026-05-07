namespace PigeonRacing.Application.Common;

/// <summary>
/// Functional result wrapper — no exceptions for business failures.
/// </summary>
public class Result
{
    public bool IsSuccess { get; protected set; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; protected set; } = string.Empty;
    public string ErrorCode { get; protected set; } = string.Empty;

    protected Result(bool isSuccess, string error, string errorCode = "")
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error, string errorCode = "ERROR") => new(false, error, errorCode);
    public static Result NotFound(string resource) => new(false, $"{resource} not found.", "NOT_FOUND");
    public static Result Forbidden() => new(false, "Access denied.", "FORBIDDEN");
    public static Result Conflict(string message) => new(false, message, "CONFLICT");
    public static Result ValidationError(string message) => new(false, message, "VALIDATION_ERROR");

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string error, string errorCode = "ERROR") => Result<T>.Failure(error, errorCode);
    public static Result<T> NotFound<T>(string resource) => Result<T>.NotFound(resource);
}

public class Result<T> : Result
{
    public T? Value { get; private set; }

    private Result(bool isSuccess, T? value, string error, string errorCode = "")
        : base(isSuccess, error, errorCode)
    {
        Value = value;
    }

    public new static Result<T> Success(T value) => new(true, value, string.Empty);
    public new static Result<T> Failure(string error, string errorCode = "ERROR") => new(false, default, error, errorCode);
    public new static Result<T> NotFound(string resource) => new(false, default, $"{resource} not found.", "NOT_FOUND");
    public new static Result<T> Forbidden() => new(false, default, "Access denied.", "FORBIDDEN");
    public new static Result<T> Conflict(string message) => new(false, default, message, "CONFLICT");
}

/// <summary>
/// Standard API response envelope.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public IEnumerable<string>? Errors { get; set; }
    public long? Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string error, string? errorCode = null, IEnumerable<string>? errors = null) =>
        new() { Success = false, Message = error, ErrorCode = errorCode, Errors = errors };
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public class PagedQuery
{
    private int _page = 1;
    private int _pageSize = 20;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > 100 ? 100 : value < 1 ? 1 : value;
    }

    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;

    public int Skip => (Page - 1) * PageSize;
}
