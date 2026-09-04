using CodeKids.Domain.Entities;

namespace CodeKids.Application.Features.Profile;

public static class ProfilePhotoUrls
{
    /// <summary>
    /// Server-relative URL for a user's photo, or null when the user has not uploaded one.
    /// The version segment changes whenever a new photo replaces the old one so browsers
    /// do not keep serving the previous image from cache.
    /// </summary>
    public static string? Build(Guid userId, string? storageKey) =>
        string.IsNullOrWhiteSpace(storageKey)
            ? null
            : $"/api/users/{userId}/photo?v={Version(storageKey)}";

    public static string? Build(User user) => Build(user.Id, user.ProfilePhotoStorageKey);

    /// <summary>FNV-1a so the value stays stable across processes, unlike string.GetHashCode.</summary>
    public static string Version(string storageKey)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;
        foreach (var c in storageKey)
        {
            hash ^= c;
            hash *= prime;
        }

        return hash.ToString("x8");
    }
}
