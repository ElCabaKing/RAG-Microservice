using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SummaryService.Api.Configurations;
using SummaryService.Api.Extensions;
using SummaryService.Api.Middleware;
using SummaryService.Application.DTOs;
using SummaryService.Application.Interfaces;
using SummaryService.Application.Validators;
using SummaryService.Domain.Constants;
using SummaryService.Domain.Enums;
using Serilog;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.Configure<GroqOptions>(builder.Configuration.GetSection(GroqOptions.SectionName));
builder.Services.Configure<SummaryOptions>(builder.Configuration.GetSection(SummaryOptions.SectionName));
builder.Services.Configure<OcrOptions>(builder.Configuration.GetSection(OcrOptions.SectionName));
builder.Services.Configure<ChunkingOptions>(builder.Configuration.GetSection(ChunkingOptions.SectionName));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<IValidator<SummaryRequestDto>, SummaryRequestValidator>();
builder.Services.AddRagServices();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = static async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"status\":\"Healthy\"}", context.RequestAborted);
    }
});

app.MapPost("/api/v1/summaries/stream", async (IFormFile file, SummaryStyle? style, int? maxTokens, HttpResponse response, ISummaryGenerator summaryGenerator, IOptions<SummaryOptions> summaryOptions, IValidator<SummaryRequestDto> validator, CancellationToken ct) =>
{
    var options = summaryOptions.Value;
    var request = new SummaryRequestDto
    {
        File = file,
        Style = style ?? SummaryStyle.General,
        MaxTokens = maxTokens ?? options.MaxTokens
    };

    var validationResult = await validator.ValidateAsync(request, ct);
    if (!validationResult.IsValid)
    {
        response.StatusCode = StatusCodes.Status400BadRequest;
        var payload = new ErrorResponseDto
        {
            Message = string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage))
        };

        response.ContentType = "application/json";
        await response.WriteAsync(JsonSerializer.Serialize(payload, jsonOptions), ct);
        return;
    }

    response.ContentType = "text/event-stream";

    await WriteSseAsync(response, SseEvents.Status, new StatusResponseDto { Status = ToSseStatus(ProcessingStatus.ExtractingText) }, ct);
    await WriteSseAsync(response, SseEvents.Status, new StatusResponseDto { Status = ToSseStatus(ProcessingStatus.ChunkingDocument) }, ct);
    await WriteSseAsync(response, SseEvents.Status, new StatusResponseDto { Status = ToSseStatus(ProcessingStatus.GeneratingSummary) }, ct);
    await WriteSseAsync(response, SseEvents.Status, new StatusResponseDto { Status = ToSseStatus(ProcessingStatus.ReducingSummary) }, ct);

    await foreach (var chunk in summaryGenerator.GenerateSummaryAsync(file.OpenReadStream(), request.Style, request.MaxTokens, ct))
    {
        await WriteSseAsync(response, SseEvents.Chunk, new SummaryChunkResponseDto { Content = chunk }, ct);
        await response.Body.FlushAsync(ct);
    }

    await WriteSseAsync(response, SseEvents.Completed, new { }, ct);
})
.Accepts<IFormFile>("multipart/form-data")
.DisableAntiforgery();

app.Run();

static string ToSseStatus(ProcessingStatus status)
{
    return status switch
    {
        ProcessingStatus.ExtractingText => "extracting_text",
        ProcessingStatus.RunningOcr => "running_ocr",
        ProcessingStatus.ChunkingDocument => "chunking_document",
        ProcessingStatus.GeneratingSummary => "generating_summary",
        ProcessingStatus.ReducingSummary => "reducing_summary",
        ProcessingStatus.Completed => "completed",
        _ => "failed"
    };
}

static Task WriteSseAsync(HttpResponse response, string eventName, object payload, CancellationToken cancellationToken)
{
    var serialized = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    return response.WriteAsync($"event: {eventName}\ndata: {serialized}\n\n", cancellationToken);
}
