namespace SummaryService.Application.DTOs;

public sealed class StreamResponseDto
{
    public string Event { get; init; } = string.Empty;

    public object Data { get; init; } = default!;
}