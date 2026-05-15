using SummaryService.Domain.Constants;

namespace SummaryService.Api.Configurations;

public sealed class GroqOptions
{
    public const string SectionName = "Groq";

    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = GroqModels.Llama33_70B;
}