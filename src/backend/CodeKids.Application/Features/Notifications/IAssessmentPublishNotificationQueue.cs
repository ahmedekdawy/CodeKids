namespace CodeKids.Application.Features.Notifications;

/// <summary>
/// Hands assessment publish notifications to a background worker so the publish request does not
/// wait for one WhatsApp round trip per student and parent.
/// </summary>
public interface IAssessmentPublishNotificationQueue
{
    void Enqueue(string? tenantId, AssessmentPublishInfo info, IReadOnlyCollection<Guid> studentIds);
}
