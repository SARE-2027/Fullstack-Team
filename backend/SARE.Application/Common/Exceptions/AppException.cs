namespace SARE.Application.Common.Exceptions;

public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public string Title { get; }
    public string? ErrorCode { get; }
    public IDictionary<string, string[]>? Errors { get; protected set; }

    protected AppException(
        string message,
        int statusCode = 400,
        string title = "خطأ في التطبيق",
        string? errorCode = null,
        IDictionary<string, string[]>? errors = null) : base(message)
    {
        StatusCode = statusCode;
        Title = title;
        ErrorCode = errorCode;
        Errors = errors;
    }
}
