using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Exams;

public sealed record PublishExamCommand(Guid TeacherUserId, Guid ExamId) : ICommand<ExamDto>;
