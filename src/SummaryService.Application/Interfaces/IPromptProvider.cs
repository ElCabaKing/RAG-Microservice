namespace SummaryService.Application.Interfaces;

public interface IPromptProvider
{
    Task<string> GetPromptAsync(string promptName, CancellationToken cancellationToken);
}