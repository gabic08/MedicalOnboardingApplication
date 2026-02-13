using Microsoft.AspNetCore.Identity;

namespace MedicalOnboardingApplication.Models;

public class ApplicationUser : IdentityUser
{
    public int? EmployeeTypeId { get; set; }
    public EmployeeType EmployeeType { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int? ClinicId { get; set; }
    public Clinic Clinic { get; set; }
    public string ProfileImagePath { get; set; }
}