using System.ComponentModel.DataAnnotations;

namespace MedicalOnboardingApplication.ViewModels;

public class UserProfileViewModel
{
    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    public string ExistingProfileImagePath { get; set; }

    public IFormFile NewProfileImage { get; set; }

    public bool RemoveProfileImage { get; set; }

    // Password section
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    [MinLength(6)]
    public string NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare("NewPassword")]
    public string? ConfirmNewPassword { get; set; }
}