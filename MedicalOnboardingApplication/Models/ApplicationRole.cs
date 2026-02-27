using Microsoft.AspNetCore.Identity;

namespace MedicalOnboardingApplication.Models;

public class ApplicationRole : IdentityRole<int>
{
    public List<ApplicationUserRole> UserRoles { get; set; } = new();
}