namespace CodeKids.Domain.Enums;

/// <summary>What kind of organisation a tenant is; the frontend tailors its menus to it.</summary>
public enum TenantStyle
{
    /// <summary>School following a curriculum: grades, timetable, certificates.</summary>
    School = 0,
    /// <summary>Academy selling standalone courses.</summary>
    Academy = 1,
    /// <summary>One teacher running their own classes.</summary>
    SingleTeacher = 2
}
