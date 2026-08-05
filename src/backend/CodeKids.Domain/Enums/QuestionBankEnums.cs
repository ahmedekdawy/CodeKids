namespace CodeKids.Domain.Enums;

public enum BankQuestionType
{
    Choose = 0,
    TrueFalse = 1,
    SingleChoice = 2,
    MultiChoice = 3,
    Paragraph = 4,
    Underline = 5
}

public enum ExamAttemptStatus
{
    InProgress = 0,
    Submitted = 1,
    Graded = 2
}
