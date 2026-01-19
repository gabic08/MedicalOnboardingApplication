namespace MedicalOnboardingApplication.Models;

public class EditQuestionViewModel
{
    public int Id { get; set; } // Question ID
    public int TestId { get; set; } // Parent Test ID

    public string QuestionText { get; set; }

    // Exactly 4 answers
    public List<string> Answers { get; set; } = new() { "", "", "", "" };

    // Index of the correct answer (0–3)
    public int CorrectAnswerIndex { get; set; }
}
