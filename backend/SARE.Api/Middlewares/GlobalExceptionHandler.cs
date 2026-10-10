using System.Diagnostics;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SARE.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using ValidationException = FluentValidation.ValidationException;

namespace SARE.Api.Middlewares;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem = exception switch
        {
            ValidationException validation => new ValidationProblemDetails(validation.Errors
                .GroupBy(error => JsonNamingPolicy.CamelCase.ConvertName(error.PropertyName))
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).Distinct().ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred."
            },
            PayloadTooLargeException => new ProblemDetails
            {
                Status = StatusCodes.Status413PayloadTooLarge, Title = "Upload too large.", Detail = exception.Message
            },
            BadHttpRequestException badRequest => new ProblemDetails
            {
                Status = badRequest.StatusCode, Title = "Invalid request.", Detail = badRequest.Message
            },
            AppException application => new ProblemDetails
            {
                Status = application.StatusCode, Title = application.Title, Detail = application.Message
            },
            DbUpdateConcurrencyException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict, Title = "Concurrent change.", Detail = "Data changed. Reload it and try again."
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError, Title = "An unexpected error occurred."
            }
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception while processing {RequestPath}.", httpContext.Request.Path);

        problem.Instance = httpContext.Request.Path;
        if (exception is AppException appException)
        {
            if (appException.ErrorCode is not null) problem.Extensions["errorCode"] = appException.ErrorCode;
            if (appException.Errors is not null) problem.Extensions["errors"] = appException.Errors
                .ToDictionary(pair => JsonNamingPolicy.CamelCase.ConvertName(pair.Key), pair => pair.Value);
        }
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        httpContext.Response.StatusCode = problem.Status!.Value;
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext, ProblemDetails = problem
        });
        return true;
    }
}
