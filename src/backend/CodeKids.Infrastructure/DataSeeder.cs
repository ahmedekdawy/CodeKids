using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Infrastructure;

public static class DataSeeder
{
    public static async Task SeedAsync(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Stages.AnyAsync(cancellationToken))
        {
            dbContext.Stages.AddRange(
                new Stage { Id = 0, Name = "رياض الأطفال", NameEn = "Kindergarten" },
                new Stage { Id = 1, Name = "المرحلة الابتدائية", NameEn = "Primary" },
                new Stage { Id = 2, Name = "المرحلة الإعدادية", NameEn = "Preparatory" },
                new Stage { Id = 3, Name = "المرحلة الثانوية", NameEn = "Secondary" });
        }

        if (!await dbContext.Grades.AnyAsync(cancellationToken))
        {
            dbContext.Grades.AddRange(
                new Grade { Id = -1, Name = "KG1", NameEn = "KG1", StageId = 0 },
                new Grade { Id = 0, Name = "KG2", NameEn = "KG2", StageId = 0 },
                new Grade { Id = 1, Name = "الصف 1", NameEn = "Grade 1", StageId = 1 },
                new Grade { Id = 2, Name = "الصف 2", NameEn = "Grade 2", StageId = 1 },
                new Grade { Id = 3, Name = "الصف 3", NameEn = "Grade 3", StageId = 1 },
                new Grade { Id = 4, Name = "الصف 4", NameEn = "Grade 4", StageId = 1 },
                new Grade { Id = 5, Name = "الصف 5", NameEn = "Grade 5", StageId = 1 },
                new Grade { Id = 6, Name = "الصف 6", NameEn = "Grade 6", StageId = 1 },
                new Grade { Id = 7, Name = "الصف 7", NameEn = "Grade 7", StageId = 2 },
                new Grade { Id = 8, Name = "الصف 8", NameEn = "Grade 8", StageId = 2 },
                new Grade { Id = 9, Name = "الصف 9", NameEn = "Grade 9", StageId = 2 },
                new Grade { Id = 10, Name = "الصف 10", NameEn = "Grade 10", StageId = 3 },
                new Grade { Id = 11, Name = "الصف 11", NameEn = "Grade 11", StageId = 3 },
                new Grade { Id = 12, Name = "الصف 12", NameEn = "Grade 12", StageId = 3 });
        }

        if (!await dbContext.Subjects.AnyAsync(cancellationToken))
        {
            dbContext.Subjects.AddRange(SubjectSeedData.All);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.Avatars.AnyAsync(cancellationToken))
        {
            dbContext.Avatars.AddRange(
                new Avatar { Id = Guid.Parse("11111111-1111-1111-1111-111111111101"), Name = "Rocket Fox", Theme = "Space", AccentColor = "#FFD60A", Emoji = "🦊", UnlockXp = 0 },
                new Avatar { Id = Guid.Parse("11111111-1111-1111-1111-111111111102"), Name = "Pixel Penguin", Theme = "Ocean", AccentColor = "#5FD3BC", Emoji = "🐧", UnlockXp = 40 },
                new Avatar { Id = Guid.Parse("11111111-1111-1111-1111-111111111103"), Name = "Code Dragon", Theme = "Fantasy", AccentColor = "#FF9F1C", Emoji = "🐉", UnlockXp = 100 });
        }

        if (!await dbContext.Badges.AnyAsync(cancellationToken))
        {
            dbContext.Badges.AddRange(
                new Badge { Id = Guid.NewGuid(), Code = "FIRST_STEP", Name = "First Step", Description = "Complete your first coding step.", Icon = "⭐", RequiredXp = 20, RequiredSteps = 1 },
                new Badge { Id = Guid.NewGuid(), Code = "LOOP_STAR", Name = "Loop Star", Description = "Earn 40 XP exploring loops.", Icon = "🔁", RequiredXp = 40, RequiredSteps = 2 },
                new Badge { Id = Guid.NewGuid(), Code = "QUIZ_HERO", Name = "Quiz Hero", Description = "Reach 80 XP with quizzes and lessons.", Icon = "🏆", RequiredXp = 80, RequiredSteps = 3 });
        }

        if (!await dbContext.Courses.AnyAsync(cancellationToken))
        {
            var starterCourseId = Guid.Parse("22222222-2222-2222-2222-222222222201");
            var logicCourseId = Guid.Parse("22222222-2222-2222-2222-222222222202");
            var starterUnitId = Guid.Parse("26262626-2626-2626-2626-262626262601");
            var logicUnitId = Guid.Parse("26262626-2626-2626-2626-262626262602");
            var loopsLessonId = Guid.Parse("33333333-3333-3333-3333-333333333301");
            var variablesLessonId = Guid.Parse("33333333-3333-3333-3333-333333333302");
            var conditionsLessonId = Guid.Parse("33333333-3333-3333-3333-333333333303");

            dbContext.Courses.AddRange(
                new Course
                {
                    Id = starterCourseId,
                    Title = "Starter Adventures",
                    Theme = "Robots & Pirates",
                    Description = "First coding missions for ages 8–10: loops and variables.",
                    AgeMin = 8,
                    AgeMax = 10,
                    TermId = CourseTerm.FirstTerm,
                    Grade = 3,
                    StageId = 1,
                    SortOrder = 1,
                    Units =
                    [
                        new CourseUnit
                        {
                            Id = starterUnitId,
                            CourseId = starterCourseId,
                            Title = "Loops and variables",
                            Description = "Loops and variables",
                            SortOrder = 1,
                            Lessons =
                            [
                                new Lesson
                                {
                                    Id = loopsLessonId,
                                    CourseId = starterCourseId,
                                    UnitId = starterUnitId,
                                    Title = "Robot Loops Adventure",
                                    Theme = "Robots",
                                    Description = "Teach a tiny robot to repeat moves using simple loops.",
                                    Difficulty = 1,
                                    XpReward = 40,
                                    SortOrder = 1,
                                    Steps =
                                    [
                                        new LessonStep
                                        {
                                            Id = Guid.Parse("44444444-4444-4444-4444-444444444401"),
                                            LessonId = loopsLessonId,
                                            StepNumber = 1,
                                            Title = "Repeat Forward",
                                            Prompt = "Type LOOP 3 FORWARD to move the robot three times.",
                                            ExpectedAnswer = "LOOP 3 FORWARD"
                                        },
                                        new LessonStep
                                        {
                                            Id = Guid.Parse("44444444-4444-4444-4444-444444444402"),
                                            LessonId = loopsLessonId,
                                            StepNumber = 2,
                                            Title = "Repeat Jump",
                                            Prompt = "Type LOOP 2 JUMP to help the robot hop twice.",
                                            ExpectedAnswer = "LOOP 2 JUMP"
                                        }
                                    ]
                                },
                                new Lesson
                                {
                                    Id = variablesLessonId,
                                    CourseId = starterCourseId,
                                    UnitId = starterUnitId,
                                    Title = "Treasure Variables",
                                    Theme = "Pirates",
                                    Description = "Learn how names can store values like treasure counts.",
                                    Difficulty = 2,
                                    XpReward = 60,
                                    SortOrder = 2,
                                    Steps =
                                    [
                                        new LessonStep
                                        {
                                            Id = Guid.Parse("44444444-4444-4444-4444-444444444403"),
                                            LessonId = variablesLessonId,
                                            StepNumber = 1,
                                            Title = "Create a Variable",
                                            Prompt = "Type coins = 5 to store five gold coins.",
                                            ExpectedAnswer = "coins = 5"
                                        },
                                        new LessonStep
                                        {
                                            Id = Guid.Parse("44444444-4444-4444-4444-444444444404"),
                                            LessonId = variablesLessonId,
                                            StepNumber = 2,
                                            Title = "Use a Variable",
                                            Prompt = "Type print(coins) to show the treasure amount.",
                                            ExpectedAnswer = "print(coins)"
                                        }
                                    ]
                                }
                            ]
                        }
                    ],
                    Quizzes =
                    [
                        new Quiz
                        {
                            Id = Guid.Parse("55555555-5555-5555-5555-555555555501"),
                            CourseId = starterCourseId,
                            Title = "Starter Checkpoint Quiz",
                            Description = "Quick check on loops and variables.",
                            XpReward = 30,
                            Questions =
                            [
                                new QuizQuestion
                                {
                                    Id = Guid.NewGuid(),
                                    QuizId = Guid.Parse("55555555-5555-5555-5555-555555555501"),
                                    Prompt = "What does a loop help a robot do?",
                                    OptionA = "Repeat an action",
                                    OptionB = "Delete code",
                                    OptionC = "Turn off the computer",
                                    CorrectOption = "A",
                                    SortOrder = 1
                                },
                                new QuizQuestion
                                {
                                    Id = Guid.NewGuid(),
                                    QuizId = Guid.Parse("55555555-5555-5555-5555-555555555501"),
                                    Prompt = "What is a variable used for?",
                                    OptionA = "Drawing pictures",
                                    OptionB = "Storing a value with a name",
                                    OptionC = "Sending email",
                                    CorrectOption = "B",
                                    SortOrder = 2
                                }
                            ]
                        }
                    ]
                },
                new Course
                {
                    Id = logicCourseId,
                    Title = "Logic Explorers",
                    Theme = "Space Missions",
                    Description = "Next-level thinking for ages 10–12 with conditions.",
                    AgeMin = 10,
                    AgeMax = 12,
                    TermId = CourseTerm.FullYear,
                    Grade = 5,
                    StageId = 1,
                    SortOrder = 2,
                    Units =
                    [
                        new CourseUnit
                        {
                            Id = logicUnitId,
                            CourseId = logicCourseId,
                            Title = "Conditions",
                            Description = "Conditions",
                            SortOrder = 1,
                            Lessons =
                            [
                                new Lesson
                                {
                                    Id = conditionsLessonId,
                                    CourseId = logicCourseId,
                                    UnitId = logicUnitId,
                                    Title = "Starship Conditions",
                                    Theme = "Space",
                                    Description = "Use IF statements to help a starship make choices.",
                                    Difficulty = 3,
                                    XpReward = 80,
                                    SortOrder = 1,
                                    Steps =
                                    [
                                        new LessonStep
                                        {
                                            Id = Guid.Parse("44444444-4444-4444-4444-444444444405"),
                                            LessonId = conditionsLessonId,
                                            StepNumber = 1,
                                            Title = "If Fuel Low",
                                            Prompt = "Type IF fuel < 10 THEN refuel to keep flying safely.",
                                            ExpectedAnswer = "IF fuel < 10 THEN refuel"
                                        },
                                        new LessonStep
                                        {
                                            Id = Guid.Parse("44444444-4444-4444-4444-444444444406"),
                                            LessonId = conditionsLessonId,
                                            StepNumber = 2,
                                            Title = "If Asteroid Ahead",
                                            Prompt = "Type IF asteroid THEN dodge to avoid a crash.",
                                            ExpectedAnswer = "IF asteroid THEN dodge"
                                        }
                                    ]
                                }
                            ]
                        }
                    ],
                    Quizzes =
                    [
                        new Quiz
                        {
                            Id = Guid.Parse("55555555-5555-5555-5555-555555555502"),
                            CourseId = logicCourseId,
                            Title = "Conditions Quiz",
                            Description = "Check your IF-statement skills.",
                            XpReward = 40,
                            Questions =
                            [
                                new QuizQuestion
                                {
                                    Id = Guid.NewGuid(),
                                    QuizId = Guid.Parse("55555555-5555-5555-5555-555555555502"),
                                    Prompt = "An IF statement helps a program...",
                                    OptionA = "Make a choice",
                                    OptionB = "Print only errors",
                                    OptionC = "Ignore all inputs",
                                    CorrectOption = "A",
                                    SortOrder = 1
                                }
                            ]
                        }
                    ]
                });
        }

        await EgyptianCurriculumSeedData.SeedAsync(dbContext, cancellationToken);

        if (!await dbContext.Users.AnyAsync(cancellationToken))
        {
            var adminId = Guid.Parse("66666666-6666-6666-6666-666666666600");
            var teacherId = Guid.Parse("66666666-6666-6666-6666-666666666601");
            var parentId = Guid.Parse("66666666-6666-6666-6666-666666666602");
            var studentId = Guid.Parse("66666666-6666-6666-6666-666666666603");
            var defaultAvatarId = Guid.Parse("11111111-1111-1111-1111-111111111101");

            dbContext.Users.AddRange(
                new User
                {
                    Id = adminId,
                    Email = "admin@codekids.local",
                    DisplayName = "Super Admin",
                    PasswordHash = passwordHasher.Hash("Admin123!"),
                    Role = UserRole.SuperAdmin,
                    TotalXp = 0
                },
                new User
                {
                    Id = teacherId,
                    Email = "teacher@codekids.local",
                    DisplayName = "Ms. Nova",
                    PasswordHash = passwordHasher.Hash("Teacher123!"),
                    Role = UserRole.Teacher,
                    TotalXp = 0
                },
                new User
                {
                    Id = parentId,
                    Email = "parent@codekids.local",
                    DisplayName = "Alex Parent",
                    PasswordHash = passwordHasher.Hash("Parent123!"),
                    Role = UserRole.Parent,
                    TotalXp = 0
                },
                new User
                {
                    Id = studentId,
                    Email = "student@codekids.local",
                    DisplayName = "Ava",
                    PasswordHash = passwordHasher.Hash("Student123!"),
                    Role = UserRole.Student,
                    ParentId = parentId,
                    AvatarId = defaultAvatarId,
                    Grade = 3,
                    TotalXp = 0
                });
        }
        else if (!await dbContext.Users.AnyAsync(x => x.Role == UserRole.SuperAdmin, cancellationToken))
        {
            dbContext.Users.Add(new User
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666600"),
                Email = "admin@codekids.local",
                DisplayName = "Super Admin",
                PasswordHash = passwordHasher.Hash("Admin123!"),
                Role = UserRole.SuperAdmin,
                TotalXp = 0
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (!await dbContext.Classrooms.AnyAsync(cancellationToken))
        {
            var teacherId = Guid.Parse("66666666-6666-6666-6666-666666666601");
            var studentId = Guid.Parse("66666666-6666-6666-6666-666666666603");
            var starterCourseId = Guid.Parse("22222222-2222-2222-2222-222222222201");
            var classroomId = Guid.Parse("77777777-7777-7777-7777-777777777701");

            var teacherExists = await dbContext.Users.AnyAsync(x => x.Id == teacherId, cancellationToken);
            var studentExists = await dbContext.Users.AnyAsync(x => x.Id == studentId, cancellationToken);
            var courseExists = await dbContext.Courses.AnyAsync(x => x.Id == starterCourseId, cancellationToken);

            if (teacherExists && studentExists)
            {
                dbContext.Classrooms.Add(new Classroom
                {
                    Id = classroomId,
                    Name = "Rocket Room A",
                    Description = "Starter coding classroom",
                    Grade = 3,
                    CourseId = courseExists ? starterCourseId : null,
                    WhatsAppGroupInviteUrl = "",
                    WhatsAppNotifyPhones = "",
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
                if (courseExists)
                {
                    dbContext.ClassroomCourses.Add(new ClassroomCourse
                    {
                        Id = Guid.NewGuid(),
                        ClassroomId = classroomId,
                        CourseId = starterCourseId,
                        TeacherId = teacherId,
                        AssignedAtUtc = DateTimeOffset.UtcNow
                    });
                }
                dbContext.ClassroomStudents.Add(new ClassroomStudent
                {
                    Id = Guid.NewGuid(),
                    ClassroomId = classroomId,
                    StudentId = studentId,
                    JoinedAtUtc = DateTimeOffset.UtcNow
                });
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

