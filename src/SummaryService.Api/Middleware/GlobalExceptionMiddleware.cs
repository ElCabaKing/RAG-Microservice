using System.Text.Json;
using SummaryService.Application.DTOs;
using SummaryService.Domain.Constants;

namespace SummaryService.Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate next;
    private readonly ILogger<GlobalExceptionMiddleware> logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);

            if (context.Response.ContentType?.StartsWith("text/event-stream", StringComparison.OrdinalIgnoreCase) == true)
            {
                var errorPayload = JsonSerializer.Serialize(new ErrorResponseDto
                {
                    Message = "An unexpected error occurred."
                }, JsonOptions);

                await context.Response.WriteAsync($"event: {SseEvents.Error}\ndata: {errorPayload}\n\n");
                return;
            }

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var jsonPayload = new
            {
                status = "error",
                message = "An unexpected error occurred."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(jsonPayload, JsonOptions));
        }
    }
}