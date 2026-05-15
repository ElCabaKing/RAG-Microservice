using FluentValidation;
using SummaryService.Application.DTOs;
using SummaryService.Domain.Enums;
using SummaryService.Shared.Helpers;

namespace SummaryService.Application.Validators;

public sealed class SummaryRequestValidator : AbstractValidator<SummaryRequestDto>
{
    private const long MaxFileSizeInBytes = 15L * 1024L * 1024L;

    public SummaryRequestValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("File is required.");

        RuleFor(x => x.File.Length)
            .LessThanOrEqualTo(MaxFileSizeInBytes)
            .When(x => x.File is not null)
            .WithMessage("File exceeds 15MB.");

        RuleFor(x => x.File.ContentType)
            .Must(FileHelper.IsSupportedMimeType)
            .When(x => x.File is not null)
            .WithMessage("Unsupported mime type.");

        RuleFor(x => x.MaxTokens)
            .GreaterThan(0)
            .WithMessage("MaxTokens must be greater than 0.");

        RuleFor(x => x.Style)
            .Must(style => Enum.IsDefined(style))
            .WithMessage("Unsupported summary style.");
    }

}