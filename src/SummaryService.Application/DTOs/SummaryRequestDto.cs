using Microsoft.AspNetCore.Http;
using SummaryService.Domain.Enums;

namespace SummaryService.Application.DTOs;

public sealed class SummaryRequestDto
{
    public IFormFile File { get; init; } = default!;

    public SummaryStyle Style { get; init; }

    public int MaxTokens { get; init; }
}