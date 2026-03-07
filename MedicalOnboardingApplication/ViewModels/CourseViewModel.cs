using System.ComponentModel.DataAnnotations;

namespace MedicalOnboardingApplication.ViewModels;

public class CourseViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Titlul este obligatoriu")]
    public string Title { get; set; }

    public string Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Ordinea trebuie să fie mai mare decât 0")]
    public int Order { get; set; }

    public List<EmployeeTypeCheckbox> EmployeeTypes { get; set; }
}

public class EmployeeTypeCheckbox
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsSelected { get; set; }
}
