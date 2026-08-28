using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.StudentAsk;

public sealed record SetStudentAskEnabledCommand(
    string Scope,
    Guid Id,
    bool Enabled) : ICommand<StudentAskSettingsDto>;
