using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Controllers;

public class QuestionsController : Controller
{
    private readonly MedicalOnboardingApplicationContext _context;

    public QuestionsController(MedicalOnboardingApplicationContext context)
    {
        _context = context;
    }

    public IActionResult Create(int testId)
    {
        return View(new CreateQuestionViewModel { TestId = testId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateQuestionViewModel vm)
    {
        if (vm.Answers.Count != 4 || vm.Answers.Any(a => string.IsNullOrWhiteSpace(a)))
        {
            ModelState.AddModelError("", "All four answers must be filled in.");
        }

        if (!ModelState.IsValid)
            return View(vm);

        var question = new Question
        {
            TestId = vm.TestId,
            Text = vm.QuestionText,
            Answers = vm.Answers.Select((text, index) => new Answer
            {
                Text = text,
                IsCorrect = index == vm.CorrectAnswerIndex
            }).ToList()
        };

        _context.Questions.Add(question);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Tests", new { id = vm.TestId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var question = await _context.Questions
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
            return NotFound();

        var vm = new EditQuestionViewModel
        {
            Id = question.Id,
            TestId = question.TestId,
            QuestionText = question.Text,
            Answers = question.Answers
                             .OrderBy(a => a.Id) // optional: maintain consistent order
                             .Select(a => a.Text)
                             .ToList(),
            CorrectAnswerIndex = question.Answers
                                 .OrderBy(a => a.Id)
                                 .ToList()
                                 .FindIndex(a => a.IsCorrect)
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditQuestionViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var question = await _context.Questions
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == vm.Id);

        if (question == null)
            return NotFound();

        question.Text = vm.QuestionText;

        for (int i = 0; i < 4; i++)
        {
            question.Answers.ElementAt(i).Text = vm.Answers[i];
            question.Answers.ElementAt(i).IsCorrect = (i == vm.CorrectAnswerIndex);
        }

        _context.Questions.Update(question);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Tests", new { id = vm.TestId });
    }

    // GET: Questions/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var question = await _context.Questions
            .Include(q => q.Test)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
            return NotFound();

        return View(question);
    }

    // POST: Questions/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var question = await _context.Questions.FindAsync(id);

        if (question == null)
            return NotFound();

        var testId = question.TestId;

        _context.Questions.Remove(question);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Tests", new { id = testId });
    }

}
