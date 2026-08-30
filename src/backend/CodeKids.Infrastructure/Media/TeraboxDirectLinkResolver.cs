using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Media;

namespace CodeKids.Infrastructure.Media;

public sealed class TeraboxDirectLinkResolver(TeraboxClient teraboxClient) : ITeraboxDirectLinkResolver
{
    public async Task<string?> TryResolveAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!TeraboxStorageKey.TryParse(storageKey, out var fsId, out _))
        {
            return null;
        }

        try
        {
            return await teraboxClient.GetDirectLinkAsync(fsId, cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}

public sealed class NullTeraboxDirectLinkResolver : ITeraboxDirectLinkResolver
{
    public Task<string?> TryResolveAsync(string storageKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}
