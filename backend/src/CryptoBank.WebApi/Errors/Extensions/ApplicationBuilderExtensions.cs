using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using CryptoBank.WebApi.Errors.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CryptoBank.WebApi.Errors.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder MapProblemDetails(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(builder =>
        {
            builder.Run(async context =>
            {
                var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>()!;
                var exception = exceptionHandlerPathFeature.Error;

                switch (exception)
                {
                    case NotFoundErrorException notFoundErrorException:
                        var notFoundProblemDetails = new ProblemDetails
                        {
                            Title = "Not found",
                            Type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/404",
                            Detail = notFoundErrorException.Message,
                            Status = (int)HttpStatusCode.NotFound,
                        };

                        await WriteErrorResponse(context, notFoundProblemDetails);
                        break;
                    case ValidationErrorsException validationErrorsException:
                    {
                        var validationProblemDetails = new ProblemDetails
                        {
                            Title = "Validation failed",
                            Type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/400",
                            Detail = validationErrorsException.Message,
                            Status = (int)HttpStatusCode.BadRequest,
                            Extensions =
                            {
                                ["errors"] = validationErrorsException.Errors
                                    .Select(x => new ErrorData(x.Field, x.Message, x.Code))
                            }
                        };

                        await WriteErrorResponse(context, validationProblemDetails);
                        break;
                    }
                    case LogicConflictException logicConflictException:
                        var logicConflictProblemDetails = new ProblemDetails
                        {
                            Title = "Logic conflict",
                            Type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/422",
                            Detail = logicConflictException.Message,
                            Status = (int)HttpStatusCode.UnprocessableEntity,
                            Extensions =
                            {
                                ["code"] = logicConflictException.Code
                            }
                        };

                        await WriteErrorResponse(context, logicConflictProblemDetails);
                        break;
                    case OperationCanceledException:
                        var operationCanceledProblemDetails = new ProblemDetails
                        {
                            Title = "Timeout",
                            Type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/504",
                            Detail = "Request timed out",
                            Status = (int)HttpStatusCode.GatewayTimeout,
                        };

                        await WriteErrorResponse(context, operationCanceledProblemDetails);
                        break;
                    default:
                        var internalErrorProblemDetails = new ProblemDetails
                        {
                            Title = "Internal server error",
                            Type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/500",
                            Detail = "Interval server error has occured",
                            Status = (int)HttpStatusCode.InternalServerError,
                        };

                        await WriteErrorResponse(context, internalErrorProblemDetails);
                        break;
                }
            });
        });

        return app;
    }

    private static async Task WriteErrorResponse(HttpContext context, ProblemDetails problemDetails)
    {
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problemDetails.Status!.Value;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }
}

internal record ErrorData(
    [property: JsonPropertyName("field")] string Field,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("code")] string Code);
