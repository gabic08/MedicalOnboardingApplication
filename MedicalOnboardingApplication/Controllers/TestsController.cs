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
}
