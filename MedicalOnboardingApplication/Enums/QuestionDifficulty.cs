using System.ComponentModel.DataAnnotations;

namespace MedicalOnboardingApplication.Enums;

public enum QuestionDifficulty
{
    [Display(Name = "Foarte Ușor")]
    VeryEasy = 1,

    [Display(Name = "Ușor")]
    Easy = 2,

    [Display(Name = "Mediu")]
    Medium = 3,

    [Display(Name = "Dificil")]
    Hard = 4,

    [Display(Name = "Foarte Dificil")]
    VeryHard = 5
}