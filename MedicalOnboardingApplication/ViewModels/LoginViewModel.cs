using System.ComponentModel.DataAnnotations;

namespace MedicalOnboardingApplication.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Emailul este obligatoriu")]
    [EmailAddress(ErrorMessage = "Adresă de email invalidă")]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Parola este obligatorie")]
    [DataType(DataType.Password)]
    [Display(Name = "Parolă")]
    public string Password { get; set; }

    public string ReturnUrl { get; set; }
}