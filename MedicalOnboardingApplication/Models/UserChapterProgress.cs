namespace MedicalOnboardingApplication.Models;

public class UserChapterProgress
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public ApplicationUser User { get; set; }
    public int ChapterId { get; set; }
    public Chapter Chapter { get; set; }
    public DateTime CompletedAt { get; set; }
}