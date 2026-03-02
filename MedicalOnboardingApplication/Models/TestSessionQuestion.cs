namespace MedicalOnboardingApplication.Models;

public class TestSessionQuestion
{
    public int Id { get; set; }
    public int TestSessionId { get; set; }
    public TestSession TestSession { get; set; }
    public int QuestionId { get; set; }
    public Question Question { get; set; }
    public int? SelectedAnswerId { get; set; }
    public Answer SelectedAnswer { get; set; }
    public bool? IsCorrect { get; set; }
    public int Order { get; set; }
    public int DifficultyAtTime { get; set; }
}
