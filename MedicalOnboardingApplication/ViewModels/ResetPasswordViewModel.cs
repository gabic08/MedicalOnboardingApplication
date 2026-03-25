using System.ComponentModel.DataAnnotations;

namespace MedicalOnboardingApplication.ViewModels;

public class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Parolă nouă")]
    public string Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirmă parola")]
    [Compare("Password", ErrorMessage = "Parolele nu coincid.")]
    public string ConfirmPassword { get; set; }

    public string Token { get; set; }
}