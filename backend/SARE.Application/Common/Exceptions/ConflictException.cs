namespace SARE.Application.Common.Exceptions;

public class ConflictException(string message, string? errorCode = "CONFLICT")
    : AppException(message, 409, "تعارض في حالة المورد", errorCode);
