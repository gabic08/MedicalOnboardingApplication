namespace MedicalOnboardingApplication.Models;

public class EmployeeType
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<CourseEmployeeType> CourseEmployeeTypes { get; set; }
}
