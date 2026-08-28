namespace CodeKids.Application.Features.QuestionImages;

public static class QuestionImageUrls
{
    public static string? Build(Guid? mediaAssetId) =>
        mediaAssetId is Guid id ? $"/api/question-images/{id}" : null;
}
