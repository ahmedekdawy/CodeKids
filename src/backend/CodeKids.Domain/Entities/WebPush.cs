namespace CodeKids.Domain.Entities;

/// <summary>One browser or installed app that a user has allowed to receive push notifications.</summary>
public class PushSubscription : TenantEntity
{
    public const int MaxEndpointLength = 1000;
    public const int MaxKeyLength = 200;

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    /// <summary>The push service URL for this device; unique, and effectively a secret.</summary>
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }
}

/// <summary>
/// The server's push identity (VAPID key pair), generated once per database and shared by all tenants in it.
/// Changing it invalidates every existing subscription.
/// </summary>
public class WebPushKeyPair
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
