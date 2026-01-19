namespace MedicalOnboardingApplication.Models;

public class CreateQuestionViewModel
{
    public int TestId { get; set; }
    public string QuestionText { get; set; }

    public List<string> Answers { get; set; } = new() { "", "", "", "" };

    public int CorrectAnswerIndex { get; set; }
}
