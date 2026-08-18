using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Attendance;

public sealed class ListTeacherPayrollAdjustmentsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListTeacherPayrollAdjustmentsQuery, IReadOnlyList<TeacherPayrollAdjustmentDto>>
{
    public async Task<IReadOnlyList<TeacherPayrollAdjustmentDto>> Handle(
        ListTeacherPayrollAdjustmentsQuery query,
        CancellationToken cancellationToken)
    {
        var rows = dbContext.TeacherPayrollAdjustments
            .AsNoTracking()
            .Include(x => x.Teacher)
            .AsQueryable();

        if (query.TeacherId.HasValue)
        {
            rows = rows.Where(x => x.TeacherId == query.TeacherId.Value);
        }

        if (query.FromDate.HasValue)
        {
            rows = rows.Where(x => x.AdjustmentDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            rows = rows.Where(x => x.AdjustmentDate <= query.ToDate.Value);
        }

        return (await rows
            .OrderByDescending(x => x.AdjustmentDate)
            .ThenBy(x => x.Teacher!.DisplayName)
            .ToListAsync(cancellationToken))
            .Select(x => new TeacherPayrollAdjustmentDto(
                x.Id,
                x.TeacherId,
                x.Teacher?.DisplayName ?? string.Empty,
                x.Amount,
                x.AdjustmentDate,
                x.Notes,
                x.CreatedAtUtc))
            .ToList();
    }
}

public sealed class CreateTeacherPayrollAdjustmentCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateTeacherPayrollAdjustmentCommand, TeacherPayrollAdjustmentDto>
{
    public async Task<TeacherPayrollAdjustmentDto> Handle(
        CreateTeacherPayrollAdjustmentCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Amount == 0)
        {
            throw new InvalidOperationException("Adjustment amount cannot be zero.");
        }

        if (command.AdjustmentDate == default)
        {
            throw new InvalidOperationException("Adjustment date is required.");
        }

        var teacher = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.TeacherId && x.Role == UserRole.Teacher, cancellationToken)
            ?? throw new InvalidOperationException("Teacher account not found.");

        var notes = (command.Notes ?? string.Empty).Trim();
        if (notes.Length > 500)
        {
            notes = notes[..500];
        }

        var row = new TeacherPayrollAdjustment
        {
            Id = Guid.NewGuid(),
            TeacherId = command.TeacherId,
            Amount = Math.Round(command.Amount, 2, MidpointRounding.AwayFromZero),
            AdjustmentDate = command.AdjustmentDate,
            Notes = notes,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.TeacherPayrollAdjustments.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TeacherPayrollAdjustmentDto(
            row.Id,
            row.TeacherId,
            teacher.DisplayName,
            row.Amount,
            row.AdjustmentDate,
            row.Notes,
            row.CreatedAtUtc);
    }
}

public sealed class DeleteTeacherPayrollAdjustmentCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteTeacherPayrollAdjustmentCommand, bool>
{
    public async Task<bool> Handle(
        DeleteTeacherPayrollAdjustmentCommand command,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.TeacherPayrollAdjustments
            .FirstOrDefaultAsync(x => x.Id == command.AdjustmentId, cancellationToken)
            ?? throw new InvalidOperationException("Payroll adjustment not found.");

        dbContext.TeacherPayrollAdjustments.Remove(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
