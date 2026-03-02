using MedicalOnboardingApplication.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Controllers;

[Authorize(Roles = "Employee")]
public class DashboardController : Controller
{
    private readonly MedicalOnboardingApplicationContext _context;

    public DashboardController(MedicalOnboardingApplicationContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _context.Users
            .Include(u => u.EmployeeType)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        var assignedCourses = await _context.Courses
            .Where(c => c.CourseEmployeeTypes
                .Any(cet => cet.EmployeeTypeId == user.EmployeeTypeId))
            .ToListAsync();

        var completedCourseIds = await _context.UserCourseProgress
            .Where(p => p.UserId == user.Id)
            .Select(p => p.CourseId)
            .ToListAsync();

        bool allCoursesCompleted = assignedCourses.Any() &&
            assignedCourses.All(c => completedCourseIds.Contains(c.Id));

        return View();
    }
}