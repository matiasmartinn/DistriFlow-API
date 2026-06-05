namespace DistriFlow.Application.Common.Results;

public sealed record Error(ErrorType Type, string Code, string Message)
{
    public static readonly Error None = new(ErrorType.None, string.Empty, string.Empty);

    public static Error Validation(string code, string message)
        => new(ErrorType.Validation, code, message);

    public static Error Unauthorized(string code, string message)
        => new(ErrorType.Unauthorized, code, message);

    public static Error Forbidden(string code, string message)
        => new(ErrorType.Forbidden, code, message);

    public static Error NotFound(string code, string message)
        => new(ErrorType.NotFound, code, message);

    public static Error Conflict(string code, string message)
        => new(ErrorType.Conflict, code, message);

    public static Error Domain(string code, string message)
        => new(ErrorType.Domain, code, message);

    public static Error Unexpected(string code, string message)
        => new(ErrorType.Unexpected, code, message);
}
