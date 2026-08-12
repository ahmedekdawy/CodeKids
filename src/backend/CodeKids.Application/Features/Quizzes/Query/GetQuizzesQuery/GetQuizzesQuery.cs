using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Quizzes;

public sealed record GetQuizzesQuery(Guid? CourseId = null, Guid? ClassroomId = null) : IQuery<IReadOnlyList<QuizDto>>;
