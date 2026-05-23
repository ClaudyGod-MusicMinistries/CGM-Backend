namespace ClaudyGod.Application.Common.Models;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public IEnumerable<string> Errors { get; init; } = [];
    public IDictionary<string, string[]> FieldErrors { get; init; } = new Dictionary<string, string[]>();

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(
        string message,
        IEnumerable<string>? errors = null,
        IDictionary<string, string[]>? fieldErrors = null) =>
        new()
        {
            Success = false,
            Message = message,
            Errors = errors ?? [],
            FieldErrors = fieldErrors ?? new Dictionary<string, string[]>()
        };
}

public class ApiResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IEnumerable<string> Errors { get; init; } = [];
    public IDictionary<string, string[]> FieldErrors { get; init; } = new Dictionary<string, string[]>();

    public static ApiResponse Ok(string? message = null) =>
        new() { Success = true, Message = message };

    public static ApiResponse Fail(
        string message,
        IEnumerable<string>? errors = null,
        IDictionary<string, string[]>? fieldErrors = null) =>
        new()
        {
            Success = false,
            Message = message,
            Errors = errors ?? [],
            FieldErrors = fieldErrors ?? new Dictionary<string, string[]>()
        };
}
