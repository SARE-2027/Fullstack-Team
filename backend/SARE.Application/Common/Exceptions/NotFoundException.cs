namespace SARE.Application.Common.Exceptions;

public class NotFoundException : AppException
{
    public NotFoundException(string message, string? errorCode = "NOT_FOUND")
        : base(message, 404, "المورد غير موجود", errorCode)
    {
    }

    public NotFoundException(string entityName, object key, string? errorCode = "NOT_FOUND")
        : base($"المورد '{entityName}' بالمعرف ({key}) غير موجود", 404, "المورد غير موجود", errorCode)
    {
    }
}
