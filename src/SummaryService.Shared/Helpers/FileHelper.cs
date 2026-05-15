using SummaryService.Domain.Constants;
using SummaryService.Domain.Enums;

namespace SummaryService.Shared.Helpers;

public static class FileHelper
{
    public static bool IsSupportedMimeType(string? contentType)
    {
        return string.Equals(contentType, MimeTypes.Pdf, StringComparison.OrdinalIgnoreCase)
            || string.Equals(contentType, MimeTypes.TextPlain, StringComparison.OrdinalIgnoreCase);
    }

    public static DocumentType? GetDocumentTypeFromExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentType.Pdf;
        }

        if (string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentType.Txt;
        }

        return null;
    }
}