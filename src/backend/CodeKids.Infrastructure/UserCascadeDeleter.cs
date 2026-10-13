using CodeKids.Application.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CodeKids.Infrastructure;

/// <summary>
/// Walks the EF model's foreign keys from Users outwards and deletes dependents before their principals,
/// so no hand-kept list of tables can fall out of date when a new table references a user.
/// </summary>
public sealed class UserCascadeDeleter(AppDbContext dbContext) : IUserCascadeDeleter
{
    private sealed record Statement(string Table, bool IsDelete, string Sql);

    public async Task<IReadOnlyDictionary<string, int>> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userType = dbContext.Model.FindEntityType(typeof(User))
            ?? throw new InvalidOperationException("User entity is not mapped.");
        var userTable = TableOf(userType);

        var statements = new List<Statement>();
        // {0} is the user id parameter; every nested condition is built on top of this one.
        var isTheUser = $"{Quote(ColumnOf(userType.FindPrimaryKey()!.Properties[0], userType))} = {{0}}";
        AddDependents(userType, isTheUser, [userType], statements);
        statements.Add(new Statement(userTable, true, $"DELETE FROM {Quote(userTable)} WHERE {isTheUser}"));

        var deleted = new Dictionary<string, int>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        foreach (var statement in statements)
        {
            var rows = await dbContext.Database.ExecuteSqlRawAsync(statement.Sql, [userId], cancellationToken);
            if (statement.IsDelete && rows > 0)
            {
                deleted[statement.Table] = deleted.GetValueOrDefault(statement.Table) + rows;
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return deleted;
    }

    /// <summary>
    /// Appends, for every foreign key pointing at <paramref name="principal"/>, the statements that clear
    /// the rows referencing the principal rows matched by <paramref name="principalFilter"/>.
    /// </summary>
    private void AddDependents(
        IEntityType principal,
        string principalFilter,
        IReadOnlyList<IEntityType> path,
        List<Statement> statements)
    {
        var principalTable = TableOf(principal);
        foreach (var foreignKey in principal.GetReferencingForeignKeys())
        {
            var dependent = foreignKey.DeclaringEntityType;
            if (dependent.GetTableName() is null)
            {
                continue;
            }

            var dependentTable = TableOf(dependent);
            var dependentColumns = foreignKey.Properties.Select(x => Quote(ColumnOf(x, dependent))).ToList();
            var principalColumns = foreignKey.PrincipalKey.Properties.Select(x => Quote(ColumnOf(x, principal))).ToList();
            var references =
                $"({string.Join(", ", dependentColumns)}) IN (SELECT {string.Join(", ", principalColumns)} FROM {Quote(principalTable)} WHERE {principalFilter})";

            var optional = foreignKey.Properties.All(x => x.IsNullable);
            if (optional)
            {
                // The row only mentions the principal (e.g. a student's parent): keep it, drop the link.
                statements.Add(new Statement(
                    dependentTable,
                    false,
                    $"UPDATE {Quote(dependentTable)} SET {string.Join(", ", dependentColumns.Select(x => $"{x} = NULL"))} WHERE {references}"));
                continue;
            }

            // A required link back into a table already on this path would recurse forever; the rows
            // it covers are removed by the delete higher up the path.
            if (path.Contains(dependent))
            {
                continue;
            }

            AddDependents(dependent, references, [.. path, dependent], statements);
            statements.Add(new Statement(dependentTable, true, $"DELETE FROM {Quote(dependentTable)} WHERE {references}"));
        }
    }

    private static string TableOf(IEntityType type) =>
        type.GetTableName() ?? throw new InvalidOperationException($"{type.DisplayName()} is not mapped to a table.");

    private static string ColumnOf(IProperty property, IEntityType type) =>
        property.GetColumnName(StoreObjectIdentifier.Table(TableOf(type), type.GetSchema()))
        ?? throw new InvalidOperationException($"{type.DisplayName()}.{property.Name} is not mapped to a column.");

    private static string Quote(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";
}
