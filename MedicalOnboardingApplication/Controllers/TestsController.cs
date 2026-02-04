using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Controllers;

public class TestsController : Controller
{
    private readonly MedicalOnboardingApplicationContext _context;

    public TestsController(MedicalOnboardingApplicationContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int courseId)
    {
        var tests = await _context.Tests
            .Where(t => t.CourseId == courseId)
            .Include(t => t.Questions)
            .ToListAsync();

        ViewBag.CourseId = courseId;
        return View(tests);
    }

    public IActionResult Create(int courseId)
    {
        return View(new Test { CourseId = courseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Test test)
    {
        if (!ModelState.IsValid)
            return View(test);

        _context.Tests.Add(test);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = test.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var test = await _context.Tests
            .Include(t => t.Questions)
                .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (test == null)
            return NotFound();

        return View(test);
    }

    // GET: Tests/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var test = await _context.Tests.FindAsync(id);
        if (test == null)
            return NotFound();

        return View(test);
    }

    // POST: Tests/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Test test)
    {
        if (id != test.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(test);

        _context.Update(test);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = test.Id });
    }

    // GET: Tests/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var test = await _context.Tests
            .Include(t => t.Questions)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (test == null)
            return NotFound();

        return View(test);
    }

    // POST: Tests/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var test = await _context.Tests
            .Include(t => t.Questions)
                .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (test != null)
        {
            _context.Tests.Remove(test);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Manage", "AdminCourses", new { id = test.CourseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDetails(int id, string title, string description)
    {
        var test = await _context.Tests.FindAsync(id);
        if (test == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(title))
        {
            ModelState.AddModelError("Title", "Title is required.");
            return await Details(id);
        }

        test.Title = title.Trim();
        test.Description = description?.Trim();

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id });
    }
}
