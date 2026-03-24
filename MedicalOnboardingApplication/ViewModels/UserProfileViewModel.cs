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
    [Display(Name = "Parola Curentă")]
    public string CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    [MinLength(6)]
    [Display(Name = "Parola Nouă")]
    public string NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare("NewPassword")]
    [Display(Name = "Confirmă Parola Nouă")]
    public string ConfirmNewPassword { get; set; }

    [Required(ErrorMessage = "Emailul este obligatoriu")]
    [EmailAddress(ErrorMessage = "Adresă de email invalidă")]
    [Display(Name = "Email")]
    public string Email { get; set; }
}