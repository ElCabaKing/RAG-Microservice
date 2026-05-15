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
using Microsoft.AspNetCore.Mvc;

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

// ========== TESTING EXTRACTORS - TEMPORARY ENDPOINT FOR DEVELOPMENT ==========
// REMOVE THIS SECTION WHEN TESTING IS COMPLETE
// Location: Add this after app.MapHealthChecks("/health", ...) in Program.cs
// Endpoint para testear los extractores de documentos sin el pipeline completo

app.MapPost("/api/v1/test/extractors/test-extractor",
async (
    [FromForm] IFormFile file,
    HttpResponse response,
    [FromServices] IDocumentProcessingService documentProcessor,
    CancellationToken ct) =>
{
    try
    {
        response.ContentType = "application/json";

        if (file == null || file.Length == 0)
        {
            response.StatusCode = StatusCodes.Status400BadRequest;

            await response.WriteAsync(
                JsonSerializer.Serialize(
                    new { error = "No file provided" },
                    jsonOptions),
                ct);

            return;
        }

        using var stream = file.OpenReadStream();

        var result = await documentProcessor.ProcessDocumentAsync(
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            ct);

        var responsePayload = new
        {
            fileName = file.FileName,
            documentType = result.Type.ToString(),
            originalSize = file.Length,
            extractedContentLength = result.Content.Length,
            normalizedContent =
                result.Content[..Math.Min(500, result.Content.Length)] +
                (result.Content.Length > 500 ? "..." : "")
        };

        response.StatusCode = StatusCodes.Status200OK;

        await response.WriteAsync(
            JsonSerializer.Serialize(responsePayload, jsonOptions),
            ct);
    }
    catch (Exception ex)
    {
        response.StatusCode = StatusCodes.Status500InternalServerError;

        var errorPayload = new
        {
            error = ex.Message
        };

        await response.WriteAsync(
            JsonSerializer.Serialize(errorPayload, jsonOptions),
            ct);
    }
})
.DisableAntiforgery()
.WithName("TestExtractors")
.WithDescription("TEST ENDPOINT - Extract text from documents without full pipeline. Remove when done testing.");
// ========== END TESTING SECTION ==========


app.MapPost("/api/v1/summaries/stream", async (HttpContext context, CancellationToken ct) =>
{
    var requestServices = context.RequestServices;
    var summaryGenerator = requestServices.GetRequiredService<ISummaryGenerator>();
    var summaryOptions = requestServices.GetRequiredService<IOptions<SummaryOptions>>();
    var validator = requestServices.GetRequiredService<IValidator<SummaryRequestDto>>();

    var form = await context.Request.ReadFormAsync(ct);
    var file = form.Files.GetFile("file");

    SummaryStyle? style = null;
    if (Enum.TryParse<SummaryStyle>(context.Request.Query["style"].ToString(), true, out var parsedStyle))
    {
        style = parsedStyle;
    }

    int? maxTokens = null;
    if (int.TryParse(context.Request.Query["maxTokens"].ToString(), out var parsedMaxTokens))
    {
        maxTokens = parsedMaxTokens;
    }

    var response = context.Response;
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
