using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Menu;

public sealed record SaveMyMenuRequest(IReadOnlyList<SaveMyMenuItemRequest> Items);

public sealed record SaveMyMenuItemRequest(string Key, bool IsHidden);

/// <summary>
/// Items are sent in the wanted menu order; anything the user's roles do not expose is rejected.
/// </summary>
public sealed record SaveMyMenuCommand(Guid UserId, IReadOnlyList<SaveMyMenuItemRequest> Items)
    : ICommand<MyMenuDto>;

public sealed record ResetMyMenuCommand(Guid UserId) : ICommand<MyMenuDto>;

public sealed class SaveMyMenuCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<SaveMyMenuCommand, MyMenuDto>
{
    public async Task<MyMenuDto> Handle(SaveMyMenuCommand command, CancellationToken cancellationToken)
    {
        var (user, roles, pages) = await MenuResolver.ResolveAsync(dbContext, command.UserId, cancellationToken);
        var pagesByKey = pages.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);

        var seen = new HashSet<Guid>();
        var ordered = new List<(Page Page, bool Hidden)>();
        foreach (var item in command.Items)
        {
            var key = (item.Key ?? string.Empty).Trim();
            if (!pagesByKey.TryGetValue(key, out var page))
            {
                throw new InvalidOperationException("The menu contains a page that is not available to this user.");
            }

            if (!seen.Add(page.Id))
            {
                throw new InvalidOperationException("The menu contains the same page twice.");
            }

            ordered.Add((page, item.IsHidden));
        }

        var preferences = await dbContext.UserMenuPreferences
            .Where(x => x.UserId == user.Id)
            .ToListAsync(cancellationToken);
        var byPage = preferences.ToDictionary(x => x.PageId);
        var keptPageIds = ordered.Select(x => x.Page.Id).ToHashSet();

        foreach (var stale in preferences.Where(x => !keptPageIds.Contains(x.PageId)))
        {
            dbContext.UserMenuPreferences.Remove(stale);
        }

        for (var index = 0; index < ordered.Count; index++)
        {
            var (page, hidden) = ordered[index];
            if (!byPage.TryGetValue(page.Id, out var preference))
            {
                preference = new UserMenuPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    PageId = page.Id
                };
                dbContext.UserMenuPreferences.Add(preference);
            }

            preference.SortOrder = index;
            preference.IsHidden = hidden;
            preference.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await dbContext.UserMenuPreferences
            .Where(x => x.UserId == user.Id)
            .ToListAsync(cancellationToken);
        return MenuResolver.ToDto(roles, pages, saved);
    }
}

public sealed class ResetMyMenuCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<ResetMyMenuCommand, MyMenuDto>
{
    public async Task<MyMenuDto> Handle(ResetMyMenuCommand command, CancellationToken cancellationToken)
    {
        var (user, roles, pages) = await MenuResolver.ResolveAsync(dbContext, command.UserId, cancellationToken);
        var preferences = await dbContext.UserMenuPreferences
            .Where(x => x.UserId == user.Id)
            .ToListAsync(cancellationToken);
        dbContext.UserMenuPreferences.RemoveRange(preferences);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MenuResolver.ToDto(roles, pages, []);
    }
}
