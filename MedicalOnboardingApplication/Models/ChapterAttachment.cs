using MedicalOnboardingApplication.Data;

namespace MedicalOnboardingApplication.Models;

public class ChapterAttachment
{
    public int Id { get; set; }

    public int ChapterId { get; set; }
    public Chapter Chapter { get; set; }

    public string FileName { get; set; }
    public string FilePath { get; set; } // null for links
    public string Url { get; set; }      // null for files

    public AttachmentType Type { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

