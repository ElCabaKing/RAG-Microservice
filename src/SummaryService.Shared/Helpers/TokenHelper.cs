namespace SummaryService.Shared.Helpers;

public static class TokenHelper
{
    public static int Estimate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return Math.Max(1, text.Length / 4);
    }
}