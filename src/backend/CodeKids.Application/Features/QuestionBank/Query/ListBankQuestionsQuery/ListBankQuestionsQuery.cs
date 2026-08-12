using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.QuestionBank;

public sealed record ListBankQuestionsQuery(Guid TeacherUserId, Guid? CourseId = null, Guid? LessonId = null)
    : IQuery<IReadOnlyList<BankQuestionDto>>;
