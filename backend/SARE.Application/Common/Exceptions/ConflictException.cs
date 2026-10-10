namespace SARE.Application.Common.Exceptions;

public class ConflictException : AppException
{
    public ConflictException(string message, string? errorCode = "CONFLICT")
        : base(message, 409, "تعارض في حالة المورد", errorCode) { }

    public ConflictException(string message, Exception innerException)
        : base(message, 409, "تعارض في حالة المورد", "CONFLICT", innerException: innerException) { }
}
