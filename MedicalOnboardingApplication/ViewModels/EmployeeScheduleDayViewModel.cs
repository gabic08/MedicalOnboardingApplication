using System.ComponentModel.DataAnnotations;

namespace MedicalOnboardingApplication.ViewModels;

public class EmployeeScheduleDayViewModel
{
    public DayOfWeek Day { get; set; }
    public bool IsDayOff { get; set; }
    public List<ShiftViewModel> Shifts { get; set; } = new();
}

public class ShiftViewModel
{
    [Required(ErrorMessage = "Ora de început este obligatorie")]
    [Display(Name = "Ora început")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "Ora de sfârșit este obligatorie")]
    [Display(Name = "Ora sfârșit")]
    public TimeOnly EndTime { get; set; }
}