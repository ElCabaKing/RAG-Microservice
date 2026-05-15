// ========== TESTING EXTRACTORS - TEMPORARY ENDPOINT FOR DEVELOPMENT ==========
// REMOVE THIS SECTION WHEN TESTING IS COMPLETE
// Location: Add this after app.MapHealthChecks("/health", ...) in Program.cs
// Endpoint para testear los extractores de documentos sin el pipeline completo

app.MapPost("/api/v1/test/extractors/test-extractor", async (IFormFile file, HttpResponse response, SummaryService.Infrastructure.Services.DocumentProcessingService documentProcessor, CancellationToken ct) =>
{
    try
    {
        response.ContentType = "application/json";

        if (file == null || file.Length == 0)
        {
            response.StatusCode = StatusCodes.Status400BadRequest;
            await response.WriteAsync(JsonSerializer.Serialize(new { error = "No file provided" }, jsonOptions), ct);
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
            normalizedContent = result.Content[..Math.Min(500, result.Content.Length)] + (result.Content.Length > 500 ? "..." : "")
        };

        response.StatusCode = StatusCodes.Status200OK;
        await response.WriteAsync(JsonSerializer.Serialize(responsePayload, jsonOptions), ct);
    }
    catch (Exception ex)
    {
        response.StatusCode = StatusCodes.Status500InternalServerError;
        var errorPayload = new { error = ex.Message };
        await response.WriteAsync(JsonSerializer.Serialize(errorPayload, jsonOptions), ct);
    }
})
.Accepts<IFormFile>("multipart/form-data")
.DisableAntiforgery()
.WithName("TestExtractors")
.WithDescription("TEST ENDPOINT - Extract text from documents without full pipeline. Remove when done testing.");

// ========== END TESTING SECTION ==========
