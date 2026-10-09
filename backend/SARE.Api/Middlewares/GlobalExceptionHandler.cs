using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SARE.Application.Common.Exceptions;

namespace SARE.Api.Middlewares;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "حدث استثناء أثناء معالجة الطلب: {Message}", exception.Message);

        int statusCode;
        string title;
        string detail;
        string? errorCode = null;
        IDictionary<string, string[]>? errors = null;

        if (exception is AppException appEx)
        {
            statusCode = appEx.StatusCode;
            title = appEx.Title;
            detail = appEx.Message;
            errorCode = appEx.ErrorCode;
            errors = appEx.Errors;
        }
        else if (exception is DbUpdateConcurrencyException)
        {
            statusCode = StatusCodes.Status409Conflict;
            title = "تعارض في التعديل المتزامن";
            detail = "حدث تعارض أثناء تعديل البيانات المتزامنة، يرجى إعادة المحاولة";
            errorCode = "CONCURRENCY_CONFLICT";
        }
        else if (exception is UnauthorizedAccessException uae)
        {
            statusCode = StatusCodes.Status401Unauthorized;
            title = "غير مصرح";
            detail = uae.Message;
            errorCode = "UNAUTHORIZED";
        }
        else
        {
            statusCode = StatusCodes.Status500InternalServerError;
            title = "خطأ داخلي في الخادم";
            detail = "حدث خطأ غير متوقع في الخادم، يرجى المحاولة لاحقاً";
            errorCode = "INTERNAL_SERVER_ERROR";
        }

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            problemDetails.Extensions["errorCode"] = errorCode;
        }

        if (errors is not null && errors.Count > 0)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
