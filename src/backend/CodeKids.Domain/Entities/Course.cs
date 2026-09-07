using CodeKids.Domain.Enums;

namespace CodeKids.Domain.Entities;

public class Course : TenantEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AgeMin { get; set; } = 8;
    public int AgeMax { get; set; } = 12;
    public CourseTerm? TermId { get; set; }
    /// <summary>Specific grade id (KG1=-1, KG2=0, grades 1–12). Null with StageId = all grades in that stage; both null = all grades.</summary>
    public int? Grade { get; set; }
    /// <summary>Optional stage. When set and Grade is null, the course covers every grade in the stage.</summary>
    public int? StageId { get; set; }
    /// <summary>Arabic, Language, or All; null treated as All.</summary>
    public SchoolType? SchoolType { get; set; }
    public int SortOrder { get; set; }
    /// <summary>External catalog subject id. Null if this course is not in the external subject list.</summary>
    public int? ExternalSubjectId { get; set; }
    /// <summary>Curriculum subject code from the MoE catalog (e.g. arabic, math).</summary>
    public string SubjectCode { get; set; } = string.Empty;
    /// <summary>core, pass_fail_non_total, activity, etc.</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>Secondary track code (science, literature, math). Empty when the grade has no tracks.</summary>
    public string TrackCode { get; set; } = string.Empty;
    public string TrackName { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = string.Empty;
    public string SourceTocUrl { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Variants { get; set; } = string.Empty;
    /// <summary>When true, students may use Ask on this course and all of its units/lessons.</summary>
    public bool StudentAskEnabled { get; set; }
    /// <summary>Draft courses stay hidden from students and parents until an admin publishes them.</summary>
    public bool IsPublished { get; set; }
    public Subject? ExternalSubject { get; set; }
    public Stage? Stage { get; set; }
    public List<Quiz> Quizzes { get; set; } = [];
}
