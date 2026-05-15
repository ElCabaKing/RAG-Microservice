namespace SummaryService.Shared.Results;

public class Result
{
    protected Result(bool isSuccess, string message, string? errorCode)
    {
        IsSuccess = isSuccess;
        Message = message;
        ErrorCode = errorCode;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public string Message { get; }

    public string? ErrorCode { get; }

    public static Result Success(string message = "Operation completed successfully.")
    {
        return new Result(true, message, null);
    }

    public static Result Failure(string message, string? errorCode = null)
    {
        return new Result(false, message, errorCode);
    }
}