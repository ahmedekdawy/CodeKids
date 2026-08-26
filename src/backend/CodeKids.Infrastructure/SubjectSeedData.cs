using CodeKids.Domain.Entities;

namespace CodeKids.Infrastructure;

internal static class SubjectSeedData
{
    public static IReadOnlyList<Subject> All { get; } =
    [
        new() { Id = 1, Title = "اللغة العربية", StageId = 2, GradeId = 9 },
        new() { Id = 7, Title = "اللغة الإنجليزية", StageId = 3, GradeId = 11 },
        new() { Id = 8, Title = "اللغة الإنجليزية", StageId = 3, GradeId = 12 },
        new() { Id = 10, Title = "Science", StageId = 2, GradeId = 9 },
        new() { Id = 11, Title = "الدراسات الاجتماعية", StageId = 2, GradeId = 9 },
        new() { Id = 12, Title = "Mathematics", StageId = 1, GradeId = 5 },
        new() { Id = 13, Title = "اللغة العربية", StageId = 2, GradeId = 7 },
        new() { Id = 14, Title = "الدراسات الاجتماعية", StageId = 2, GradeId = 7 },
        new() { Id = 15, Title = "اللغة الإنجليزية", StageId = 2, GradeId = 7 },
        new() { Id = 16, Title = "Science", StageId = 2, GradeId = 7 },
        new() { Id = 17, Title = "اللغة العربية", StageId = 2, GradeId = 8 },
        new() { Id = 19, Title = "اللغة الإنجليزية", StageId = 2, GradeId = 8 },
        new() { Id = 20, Title = "Science", StageId = 2, GradeId = 8 },
        new() { Id = 21, Title = "Mathematics", StageId = 2, GradeId = 9 },
        new() { Id = 22, Title = "اللغة العربية", StageId = 3, GradeId = 10 },
        new() { Id = 27, Title = "التاريخ", StageId = 3, GradeId = 10 },
        new() { Id = 37, Title = "العلوم", StageId = 2, GradeId = 7 },
        new() { Id = 38, Title = "العلوم", StageId = 2, GradeId = 8 },
        new() { Id = 39, Title = "العلوم", StageId = 2, GradeId = 9 },
        new() { Id = 40, Title = "اللغة العربية", StageId = 1, GradeId = 4 },
        new() { Id = 55, Title = "الرياضيات", StageId = 1, GradeId = 5 },
        new() { Id = 57, Title = "الرياضيات", StageId = 2, GradeId = 7 },
        new() { Id = 58, Title = "الرياضيات", StageId = 2, GradeId = 8 },
        new() { Id = 59, Title = "Mathematics", StageId = 2, GradeId = 7 },
        new() { Id = 60, Title = "Mathematics", StageId = 2, GradeId = 8 },
        new() { Id = 69, Title = "الرياضيات", StageId = 2, GradeId = 9 },
        new() { Id = 70, Title = "اللغة العربية", StageId = 1, GradeId = 5 },
        new() { Id = 71, Title = "اللغة الإنجليزية", StageId = 1, GradeId = 5 },
        new() { Id = 72, Title = "Science", StageId = 1, GradeId = 5 },
        new() { Id = 78, Title = "اللغة العربية", StageId = 1, GradeId = 6 },
        new() { Id = 79, Title = "اللغة الإنجليزية", StageId = 1, GradeId = 6 },
        new() { Id = 82, Title = "Science", StageId = 1, GradeId = 6 },
        new() { Id = 83, Title = "الرياضيات", StageId = 1, GradeId = 4 },
        new() { Id = 98, Title = "الرياضيات", StageId = 1, GradeId = 3 },
        new() { Id = 176, Title = "English", StageId = 1, GradeId = 2 },
        new() { Id = 179, Title = "التاريخ", StageId = 3, GradeId = 11 },
        new() { Id = 180, Title = "الجغرافيا", StageId = 3, GradeId = 11 },
        new() { Id = 193, Title = "الجغرافيا", StageId = 3, GradeId = 12 },
        new() { Id = 194, Title = "التاريخ", StageId = 3, GradeId = 12 },
        new() { Id = 261, Title = "اللغة العربية", StageId = 1, GradeId = 3 },
        new() { Id = 262, Title = "English", StageId = 1, GradeId = 3 },
        new() { Id = 355, Title = "تربية إسلامية", StageId = 1, GradeId = 5 },
        new() { Id = 356, Title = "تربية إسلامية", StageId = 1, GradeId = 4 }
    ];
}
