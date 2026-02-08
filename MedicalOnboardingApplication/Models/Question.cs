using MedicalOnboardingApplication.Data;

namespace MedicalOnboardingApplication.Models;

public class Question
{
    public int Id { get; set; }
    public string Text { get; set; }
    public QuestionDifficulty Difficulty { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; }
    public ICollection<Answer> Answers { get; set; }
}

