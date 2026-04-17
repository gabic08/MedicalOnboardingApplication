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
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Parola trebuie să aibă cel puțin 6 caractere")]
    [RegularExpression(
    @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{6,}$",
    ErrorMessage = "Parola trebuie să conțină cel puțin o literă mică, o literă mare, o cifră și un caracter special.")]
    [Display(Name = "Parolă")]
    public string Password { get; set; }

    [Required(ErrorMessage = "Confirmarea parolei este obligatorie")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Parolele nu coincid")]
    [Display(Name = "Confirmă Parola")]
    public string ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Numele clinicii este obligatoriu")]
    [StringLength(100)]
    [Display(Name = "Numele Clinicii")]
    public string ClinicName { get; set; }

    [Required(ErrorMessage = "Subdomeniul este obligatoriu")]
    [StringLength(50)]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Subdomeniul poate conține doar litere mici, cifre și cratime.")]
    [Display(Name = "Subdomeniu")]
    public string Subdomain { get; set; }

    public string ReturnUrl { get; set; }
}