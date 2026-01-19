namespace MedicalOnboardingApplication.Models;

public class CourseEmployeeType
{
    public int CourseId { get; set; }
    public Course Course { get; set; }

    public int EmployeeTypeId { get; set; }
    public EmployeeType EmployeeType { get; set; }
}
