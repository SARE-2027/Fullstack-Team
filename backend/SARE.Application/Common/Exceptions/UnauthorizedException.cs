namespace SARE.Application.Common.Exceptions;

public class UnauthorizedException(string message = "غير مصرح بتنفيذ هذا الإجراء", string? errorCode = "UNAUTHORIZED")
    : AppException(message, 401, "غير مصرح", errorCode);
