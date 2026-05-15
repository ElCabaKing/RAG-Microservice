using System.Text;

namespace SummaryService.Shared.Helpers;

public static class StreamHelper
{
    public static async Task<string> ReadAsStringAsync(Stream input, CancellationToken cancellationToken)
    {
        input.Position = 0;

        using var reader = new StreamReader(input, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var content = await reader.ReadToEndAsync(cancellationToken);
        input.Position = 0;
        return content;
    }
}