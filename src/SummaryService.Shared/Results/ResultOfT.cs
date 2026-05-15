namespace SummaryService.Shared.Results;

public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, string message, string? errorCode)
        : base(isSuccess, message, errorCode)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value, string message = "Operation completed successfully.")
    {
        return new Result<T>(true, value, message, null);
    }

    public new static Result<T> Failure(string message, string? errorCode = null)
    {
        return new Result<T>(false, default, message, errorCode);
    }
}