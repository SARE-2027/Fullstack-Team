namespace SARE.Application.Common.Exceptions;

public class BusinessRuleException(string message, string? errorCode = "BUSINESS_RULE_VIOLATION")
    : AppException(message, 400, "مخالفة لقواعد العمل", errorCode);
