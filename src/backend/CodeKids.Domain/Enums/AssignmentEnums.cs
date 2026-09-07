namespace CodeKids.Domain.Enums;

public enum AssignmentQuestionType
{
    ShortAnswer = 0,
    MultipleChoice = 1,
    Choose = 2,
    TrueFalse = 3,
    SingleChoice = 4,
    MultiChoice = 5,
    Paragraph = 6,
    Underline = 7,
    FreeText = 8
}

public enum AssignmentSubmissionStatus
{
    Submitted = 0,
    Graded = 1
}
