using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Enums;
using MedicalOnboardingApplication.Models;
using MedicalOnboardingApplication.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Controllers;

[Authorize(Roles = "Admin")]
public class QuestionsController : Controller
{
    private readonly MedicalOnboardingApplicationContext _context;

    public QuestionsController(MedicalOnboardingApplicationContext context)
    {
        _context = context;
    }

    // GET: Questions/Create?courseId=5
    public IActionResult Create(int courseId)
    {
        return View(new CreateQuestionViewModel
        {
            CourseId = courseId,
            Difficulty = QuestionDifficulty.Easy
        });
    }

    // POST: Questions/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateQuestionViewModel vm)
    {
        if (vm.Answers.Any(a => string.IsNullOrWhiteSpace(a)) == true)
        {
            ModelState.AddModelError("", "Vă rugăm să eliminați răspunsurile goale.");
        }

        if (vm.Answers.Count < 2)
        {
            ModelState.AddModelError("", "O întrebare trebuie să aibă cel puțin 2 variante de răspuns.");
        }

        if (!ModelState.IsValid)
            return View(vm);

        var question = new Question
        {
            CourseId = vm.CourseId,
            Text = vm.QuestionText,
            Difficulty = vm.Difficulty,
            Answers = vm.Answers
                .Select((text, index) => new Answer
                {
                    Text = text,
                    IsCorrect = index == vm.CorrectAnswerIndex
                })
                .ToList()
        };

        _context.Questions.Add(question);
        await _context.SaveChangesAsync();

        return RedirectToAction("Manage", "AdminCourses", new { id = vm.CourseId });
    }

    // GET: Questions/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var question = await _context.Questions
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
            return NotFound();

        var orderedAnswers = question.Answers
                                     .OrderBy(a => a.Id)
                                     .ToList();

        var vm = new EditQuestionViewModel
        {
            Id = question.Id,
            CourseId = question.CourseId,
            QuestionText = question.Text,
            Difficulty = question.Difficulty,
            Answers = orderedAnswers
                        .Select(a => new AnswerEditVm
                        {
                            Id = a.Id,
                            Text = a.Text
                        })
                        .ToList(),
            CorrectAnswerIndex = orderedAnswers.FindIndex(a => a.IsCorrect)
        };

        return View(vm);
    }

    // POST: Questions/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditQuestionViewModel vm)
    {
        // Remove any empty answers
        vm.Answers = vm.Answers
                       .Where(a => !string.IsNullOrWhiteSpace(a.Text))
                       .ToList();

        if (vm.Answers.Count < 2)
        {
            ModelState.AddModelError("", "O întrebare trebuie să aibă cel puțin 2 variante de răspuns.");
        }

        if (!ModelState.IsValid)
            return View(vm);

        var question = await _context.Questions
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == vm.Id);

        if (question == null)
            return NotFound();

        question.Text = vm.QuestionText;
        question.Difficulty = vm.Difficulty;

        // Remove old answers
        _context.Answers.RemoveRange(question.Answers);

        // Map new answers from AnswerEditVm
        question.Answers = vm.Answers
                             .Select((a, index) => new Answer
                             {
                                 Id = a.Id, // keep Id if exists, 0 for new
                                 Text = a.Text,
                                 IsCorrect = index == vm.CorrectAnswerIndex
                             })
                             .ToList();

        await _context.SaveChangesAsync();

        return RedirectToAction("Manage", "AdminCourses", new { id = vm.CourseId });
    }

    // GET: Questions/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var question = await _context.Questions
            .Include(q => q.Course)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
            return NotFound();

        return View(question);
    }

    // POST: Questions/Delete
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, int courseId)
    {
        var question = await _context.Questions
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question != null)
        {
            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Manage", "AdminCourses", new { id = courseId });
    }

}
