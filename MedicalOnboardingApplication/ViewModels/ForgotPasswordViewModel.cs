using System.ComponentModel.DataAnnotations;

namespace MedicalOnboardingApplication.ViewModels;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Emailul este obligatoriu")]
    [EmailAddress(ErrorMessage = "Adresă de email invalidă")]
    [Display(Name = "Email")]
    public string Email { get; set; }
}
