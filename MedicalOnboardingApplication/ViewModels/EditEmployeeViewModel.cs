using System.ComponentModel.DataAnnotations;

namespace MedicalOnboardingApplication.ViewModels;

public class EditEmployeeViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Prenumele este obligatoriu")]
    [Display(Name = "Prenume")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Numele este obligatoriu")]
    [Display(Name = "Nume")]
    public string LastName { get; set; }

    [Required(ErrorMessage = "Emailul este obligatoriu")]
    [EmailAddress(ErrorMessage = "Adresă de email invalidă")]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Display(Name = "Tip Angajat")]
    public int? EmployeeTypeId { get; set; }

    public List<EmployeeScheduleDayViewModel> Schedule { get; set; } = new();
}