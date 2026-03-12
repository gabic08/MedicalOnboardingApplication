using System.ComponentModel.DataAnnotations;

namespace MedicalOnboardingApplication.ViewModels;

public class RegisterAdminViewModel
{
    [Required(ErrorMessage = "Prenumele este obligatoriu")]
    [StringLength(50)]
    [Display(Name = "Prenume")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Numele este obligatoriu")]
    [StringLength(50)]
    [Display(Name = "Nume")]
    public string LastName { get; set; }

    [Required(ErrorMessage = "Emailul este obligatoriu")]
    [EmailAddress(ErrorMessage = "Adresă de email invalidă")]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Parola este obligatorie")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Parola trebuie să aibă cel puțin 8 caractere")]
    [Display(Name = "Parolă")]
    public string Password { get; set; }

    [Required(ErrorMessage = "Confirmarea parolei este obligatorie")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Parolele nu coincid")]
    [Display(Name = "Confirmă Parola")]
    public string ConfirmPassword { get; set; }

    public string ReturnUrl { get; set; }
}