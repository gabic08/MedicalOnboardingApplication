namespace MedicalOnboardingApplication.Models;

public class TestSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public ApplicationUser User { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public double AverageDifficulty { get; set; }
    public List<TestSessionQuestion> Questions { get; set; } = new();
}
