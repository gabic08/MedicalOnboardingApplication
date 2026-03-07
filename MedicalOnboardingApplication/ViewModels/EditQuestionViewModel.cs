using MedicalOnboardingApplication.Enums;

namespace MedicalOnboardingApplication.ViewModels;

public class EditQuestionViewModel
{
    public int Id { get; set; }
    public int CourseId { get; set; }

    public string QuestionText { get; set; }

    public QuestionDifficulty Difficulty { get; set; }

    public List<AnswerEditVm> Answers { get; set; } = new();

    public int CorrectAnswerIndex { get; set; }
}

public class AnswerEditVm
{
    public int Id { get; set; } // existing answer ID
    public string Text { get; set; }
}
