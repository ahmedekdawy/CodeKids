using CodeKids.Domain.Entities;

namespace CodeKids.Application.Abstractions;

public interface ICourseBookTextProvider
{
    /// <summary>
    /// Tries to extract plain text from the course's uploaded book (a PDF learning
    /// material attached at course level). Returns an empty string when the course has
    /// no PDF book or extraction fails.
    /// </summary>
    Task<string> TryGetBookTextAsync(
        Course course,
        int maxCharacters,
        CancellationToken cancellationToken = default);
}
