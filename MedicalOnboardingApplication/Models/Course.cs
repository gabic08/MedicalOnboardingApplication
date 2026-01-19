using static System.Net.Mime.MediaTypeNames;

namespace MedicalOnboardingApplication.Models;

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int Order { get; set; }
    public ICollection<Chapter> Chapters { get; set; }
    public ICollection<Test> Tests { get; set; }
}

