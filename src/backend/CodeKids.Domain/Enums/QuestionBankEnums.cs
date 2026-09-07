namespace CodeKids.Domain.Enums;

public enum BankQuestionType
{
    Choose = 0,
    TrueFalse = 1,
    SingleChoice = 2,
    MultiChoice = 3,
    Paragraph = 4,
    Underline = 5,
    FreeText = 6,
    /// <summary>Text answer with an optional model answer for auto-grading (quizzes / bank).</summary>
    ShortAnswer = 7
}

public enum ExamAttemptStatus
{
    InProgress = 0,
    Submitted = 1,
    Graded = 2
}
