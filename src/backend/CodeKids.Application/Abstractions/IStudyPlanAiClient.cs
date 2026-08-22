namespace CodeKids.Application.Abstractions;

public interface IStudyPlanAiClient
{
    Task<string> CompleteJsonAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken);
}
