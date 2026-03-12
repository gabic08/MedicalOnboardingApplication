using System.ComponentModel.DataAnnotations;

namespace MedicalOnboardingApplication.ViewModels;

public class EditChapterViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Titlul este obligatoriu")]
    public string Title { get; set; }

    public string Content { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Ordinea trebuie să fie mai mare decât 0")]
    public int Order { get; set; }

    public int CourseId { get; set; }
}