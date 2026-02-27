namespace MedicalOnboardingApplication.Models;

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int Order { get; set; }
    public ICollection<Chapter> Chapters { get; set; }
    public ICollection<Question> Questions { get; set; }
    public ICollection<CourseEmployeeType> CourseEmployeeTypes { get; set; }

    public int ClinicId { get; set; }
    public Clinic Clinic { get; set; }
}

