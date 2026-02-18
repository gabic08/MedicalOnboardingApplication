namespace MedicalOnboardingApplication.Models;

public class ApplicationUserEmployeeType
{
    public int EmployeeTypeId { get; set; }
    public EmployeeType EmployeeType { get; set; }
    public string ApplicationUserId { get; set; }
    public ApplicationUser ApplicationUser { get; set; }
}
