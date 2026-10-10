using FluentValidation.Results;

namespace SARE.Application.Common.Exceptions;

public class ValidationException : AppException
{
    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("حدث خطأ في التحقق من صحة البيانات المدخلة", 400, "خطأ في التحقق من البيانات", "VALIDATION_FAILED")
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public ValidationException(string propertyName, string errorMessage)
        : base("حدث خطأ في التحقق من صحة البيانات المدخلة", 400, "خطأ في التحقق من البيانات", "VALIDATION_FAILED")
    {
        Errors = new Dictionary<string, string[]>
        {
            { propertyName, [errorMessage] }
        };
    }
}
