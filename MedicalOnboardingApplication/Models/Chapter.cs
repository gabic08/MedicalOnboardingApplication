namespace MedicalOnboardingApplication.Models;

public class Chapter
{
    public int Id { get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; }

    public string Title { get; set; }
    public string Content { get; set; }
    public int Order { get; set; }

    public List<ChapterAttachment> Attachments { get; set; } = new();
    public List<UserChapterProgress> Progress { get; set; } = new();
}
