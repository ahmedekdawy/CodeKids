using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.StudyPlans;

public sealed record DeleteWeeklyStudyPlanCommand(Guid TeacherId, Guid PlanId) : ICommand<bool>;
