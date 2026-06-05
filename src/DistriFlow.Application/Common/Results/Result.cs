namespace DistriFlow.Application.Common.Results;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<T> : Result
{
    public T? Data { get; }

    private Result(bool isSuccess, T? data, Error error)
        : base(isSuccess, error)
    {
        Data = data;
    }

    public static Result<T> Success(T data) => new(true, data, Error.None);
    public static new Result<T> Failure(Error error) => new(false, default, error);

    public static implicit operator Result<T>(Error error) => Failure(error);
    public static implicit operator Result<T>(T data) => Success(data);
}
