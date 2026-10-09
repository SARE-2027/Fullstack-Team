namespace SARE.Application.Common.Exceptions;

public class ForbiddenException(string message = "ليس لديك صلاحية لتنفيذ هذا الإجراء", string? errorCode = "FORBIDDEN")
    : AppException(message, 403, "إجراء محظور", errorCode);
