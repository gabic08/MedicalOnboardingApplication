namespace MedicalOnboardingApplication.Models;

public class UserCourseProgress
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public ApplicationUser User { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; }
    public DateTime CompletedAt { get; set; }
}