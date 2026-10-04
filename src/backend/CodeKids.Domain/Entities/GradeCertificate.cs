namespace CodeKids.Domain.Entities;

/// <summary>Admin-defined marks certificate for every student of a classroom.</summary>
public class GradeCertificate : TenantEntity
{
    public Guid Id { get; set; }
    public Guid ClassroomId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Approved by a system admin; only approved certificates are visible to students and their parents.</summary>
    public bool IsApproved { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public Guid? ApprovedByUserId { get; set; }

    public Classroom? Classroom { get; set; }
    public List<GradeCertificateSubject> Subjects { get; set; } = [];
}

/// <summary>One subject column on a certificate, with its maximum degree.</summary>
public class GradeCertificateSubject : TenantEntity
{
    public Guid Id { get; set; }
    public Guid CertificateId { get; set; }
    public Guid CourseId { get; set; }
    public decimal MaxDegree { get; set; }
    /// <summary>False keeps the subject out of the certificate total (e.g. religion).</summary>
    public bool IncludedInTotal { get; set; } = true;
    public int SortOrder { get; set; }

    public GradeCertificate? Certificate { get; set; }
    public Course? Course { get; set; }
    public List<GradeCertificateMark> Marks { get; set; } = [];
}

/// <summary>A student's degree in one certificate subject, entered by the subject teacher or an admin.</summary>
public class GradeCertificateMark : TenantEntity
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public Guid StudentId { get; set; }
    public decimal Degree { get; set; }
    public Guid UpdatedByUserId { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public GradeCertificateSubject? Subject { get; set; }
    public User? Student { get; set; }
}
