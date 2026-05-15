namespace SummaryService.Api.Configurations;

public sealed class OcrOptions
{
    public const string SectionName = "Ocr";

    public int Dpi { get; init; } = 300;

    public string Language { get; init; } = "eng";
}