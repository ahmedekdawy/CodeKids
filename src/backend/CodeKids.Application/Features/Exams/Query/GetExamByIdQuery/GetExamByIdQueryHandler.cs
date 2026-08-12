using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Exams;

public sealed class GetExamByIdQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetExamByIdQuery, ExamDto?>
{
    public async Task<ExamDto?> Handle(GetExamByIdQuery query, CancellationToken cancellationToken)
    {
        var includeKey = string.Equals(query.ViewerRole, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase)
            || string.Equals(query.ViewerRole, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);
        return await CreateExamCommandHandler.LoadExam(dbContext, query.ExamId, includeKey, cancellationToken);
    }
}
