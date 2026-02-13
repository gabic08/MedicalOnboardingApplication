using MedicalOnboardingApplication.Data;
using System.ComponentModel.DataAnnotations;

namespace MedicalOnboardingApplication.ViewModels;

public class CreateQuestionViewModel
{
    public int CourseId { get; set; }

    [Required]
    public string QuestionText { get; set; }

    public QuestionDifficulty Difficulty { get; set; }

    public List<string> Answers { get; set; } = new() { "", "" };

    public int CorrectAnswerIndex { get; set; }
}
