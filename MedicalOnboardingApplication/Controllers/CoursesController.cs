using MedicalOnboardingApplication.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Controllers;

[Authorize] // any authenticated user
public class CoursesController : Controller
{
    private readonly MedicalOnboardingApplicationContext _context;

    public CoursesController(MedicalOnboardingApplicationContext context)
    {
        _context = context;
    }

    // GET: Courses
    public async Task<IActionResult> Index()
    {
        var courses = await _context.Courses
            .Include(c => c.CourseEmployeeTypes)
                .ThenInclude(cet => cet.EmployeeType)
            .OrderBy(c => c.Order)
            .ToListAsync();

        return View(courses);
    }

    // GET: Courses/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var course = await _context.Courses
            .Include(c => c.Chapters.OrderBy(ch => ch.Order))
            .ThenInclude(ch => ch.Attachments)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
            return NotFound();

        return View(course);
    }
}