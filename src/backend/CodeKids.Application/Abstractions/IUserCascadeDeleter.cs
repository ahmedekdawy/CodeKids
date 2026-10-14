namespace CodeKids.Application.Abstractions;

/// <summary>Deletes a user together with every row that belongs to them.</summary>
public interface IUserCascadeDeleter
{
    /// <summary>
    /// Deletes the user and, following the database relationships, all rows that require them
    /// (and the rows that require those, and so on). Rows that only point at the user through an
    /// optional link are kept and unlinked. Runs in one transaction.
    /// </summary>
    /// <returns>Rows deleted per table, for tables where anything was deleted.</returns>
    Task<IReadOnlyDictionary<string, int>> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
