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

    public async Task<IActionResult> Index(string search, string filter = "all")
    {
        if (User.IsInRole("Admin"))
        {
            return RedirectToAction("Index", "AdminCourses");
        }

        var user = await _context.Users
            .Include(u => u.EmployeeType)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        var query = _context.Courses
            .Include(c => c.CourseEmployeeTypes)
                .ThenInclude(cet => cet.EmployeeType)
            .Include(c => c.Chapters)
            .Where(c => c.CourseEmployeeTypes.Any(cet => cet.EmployeeTypeId == user.EmployeeTypeId));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.Title.ToLower().Contains(term) ||
                c.Description.ToLower().Contains(term));
        }

        var courses = await query.OrderBy(c => c.Order).ToListAsync();

        var completedCourseIds = await _context.UserCourseProgress
            .Where(p => p.UserId == user.Id)
            .Select(p => p.CourseId)
            .ToListAsync();

        var completedChapterIds = await _context.UserChapterProgress
            .Where(p => p.UserId == user.Id)
            .Select(p => p.ChapterId)
            .ToListAsync();

        courses = filter switch
        {
            "completed" => courses.Where(c => completedCourseIds.Contains(c.Id)).ToList(),
            "inprogress" => courses.Where(c =>
                !completedCourseIds.Contains(c.Id) &&
                completedChapterIds.Any(chId => c.Chapters.Any(ch => ch.Id == chId))).ToList(),
            "notstarted" => courses.Where(c =>
                !completedChapterIds.Any(chId => c.Chapters.Any(ch => ch.Id == chId))).ToList(),
            _ => courses
        };

        ViewBag.Search = search;
        ViewBag.Filter = filter;
        ViewBag.CompletedCourseIds = completedCourseIds;
        ViewBag.CompletedChapterIds = completedChapterIds;

        return View(courses);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (currentUser == null)
            return NotFound();

        var course = await _context.Courses
            .Include(c => c.Chapters)
            .Include(c => c.CourseEmployeeTypes)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
            return NotFound();

        // Must belong to the same clinic
        if (course.ClinicId != currentUser.ClinicId)
            return NotFound();

        // Employees must also be assigned to this course via their employee type
        if (!User.IsInRole("Admin"))
        {
            bool isAssigned = course.CourseEmployeeTypes
                .Any(cet => cet.EmployeeTypeId == currentUser.EmployeeTypeId);

            if (!isAssigned)
                return NotFound();

            ViewBag.CompletedChapterIds = await _context.UserChapterProgress
                .Where(p => p.UserId == currentUser.Id &&
                            course.Chapters.Select(c => c.Id).Contains(p.ChapterId))
                .Select(p => p.ChapterId)
                .ToListAsync();

            ViewBag.CompletedCourseIds = await _context.UserCourseProgress
                .Where(p => p.UserId == currentUser.Id)
                .Select(p => p.CourseId)
                .ToListAsync();
        }

        return View(course);
    }
}