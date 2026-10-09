using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SARE.Api.Middlewares;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "حدث استثناء أثناء معالجة الطلب: {Message}", exception.Message);

        var (statusCode, title, detail) = exception switch
        {
            FluentValidation.ValidationException fve => (StatusCodes.Status400BadRequest, "خطأ في التحقق من صحة البيانات", string.Join("; ", fve.Errors.Select(e => e.ErrorMessage))),
            SARE.Application.Common.Exceptions.AuthException auth => (StatusCodes.Status400BadRequest, "خطأ في المصادقة", auth.Message),
            KeyNotFoundException knf => (StatusCodes.Status404NotFound, "المورد غير موجود", knf.Message),
            UnauthorizedAccessException uae => (StatusCodes.Status401Unauthorized, "غير مصرح", uae.Message),
            InvalidOperationException ioe => (StatusCodes.Status400BadRequest, "طلب غير صالح", ioe.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "تعارض في التعديل المتزامن", "حدث تعارض أثناء تعديل البيانات المتزامنة، يرجى إعادة المحاولة"),
            ArgumentException ae => (StatusCodes.Status400BadRequest, "بيانات غير صالحة", ae.Message),
            _ => (StatusCodes.Status500InternalServerError, "خطأ داخلي في الخادم", "حدث خطأ غير متوقع في الخادم، يرجى المحاولة لاحقاً")
        };

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        if (exception is FluentValidation.ValidationException valEx)
        {
            problemDetails.Extensions["errors"] = valEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
