using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Controllers;

[Authorize(Roles = "Admin")]
[RequireClinic]
public class AdminEmployeesController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MedicalOnboardingApplicationContext _context;

    public AdminEmployeesController(
        UserManager<ApplicationUser> userManager,
        MedicalOnboardingApplicationContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index(string search, int? employeeTypeId)
    {
        var currentAdmin = await _userManager.Users
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        var query = _userManager.Users
            .Include(u => u.EmployeeType)
            .Where(u => u.ClinicId == currentAdmin.ClinicId
                     && u.UserRoles.Any(ur => ur.Role.Name != "Admin"));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term) ||
                (u.EmployeeType != null && u.EmployeeType.Name.ToLower().Contains(term)));
        }

        if (employeeTypeId.HasValue)
        {
            query = query.Where(u => u.EmployeeTypeId == employeeTypeId.Value);
        }

        var employees = await query.ToListAsync();

        ViewBag.Search = search;
        ViewBag.EmployeeTypeId = employeeTypeId;
        ViewBag.EmployeeTypes = await _context.EmployeeTypes.ToListAsync();

        return View(employees);
    }

    public IActionResult Create()
    {
        ViewBag.EmployeeTypes = new SelectList(
            _context.EmployeeTypes,
            "Id",
            "Name");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ApplicationUser model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.EmployeeTypes = new SelectList(
                _context.EmployeeTypes,
                "Id",
                "Name",
                model.EmployeeTypeId);

            return View(model);
        }

        var admin = await _userManager.GetUserAsync(User);

        model.UserName = model.Email;
        model.ClinicId = admin.ClinicId;
        model.EmailConfirmed = true;

        var result = await _userManager.CreateAsync(
            model,
            "P@ssw0rd"
        );

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(model, "Employee");
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound();

        ViewBag.EmployeeTypes = new SelectList(
            _context.EmployeeTypes,
            "Id",
            "Name",
            user.EmployeeTypeId);

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ApplicationUser model)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.EmployeeTypeId = model.EmployeeTypeId;

        await _userManager.UpdateAsync(user);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        await _userManager.DeleteAsync(user);

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Report(int id)
    {
        var admin = await _userManager.Users
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (admin?.ClinicId == null)
            return RedirectToAction("Index", "Admin");

        var employee = await _userManager.Users
            .Include(u => u.EmployeeType)
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u => u.Id == id && u.ClinicId == admin.ClinicId);

        if (employee == null)
            return NotFound();

        var assignedCourses = await _context.Courses
            .Include(c => c.Chapters)
            .Include(c => c.CourseEmployeeTypes)
            .Where(c => c.CourseEmployeeTypes
                .Any(cet => cet.EmployeeTypeId == employee.EmployeeTypeId))
            .OrderBy(c => c.Order)
            .ToListAsync();

        var completedCourseIds = await _context.UserCourseProgress
            .Where(p => p.UserId == employee.Id)
            .Select(p => p.CourseId)
            .ToListAsync();

        var completedChapterIds = await _context.UserChapterProgress
            .Where(p => p.UserId == employee.Id)
            .Select(p => p.ChapterId)
            .ToListAsync();

        var chapterProgressMap = await _context.UserChapterProgress
            .Where(p => p.UserId == employee.Id)
            .ToDictionaryAsync(p => p.ChapterId, p => p.CompletedAt);

        var courseProgressMap = await _context.UserCourseProgress
            .Where(p => p.UserId == employee.Id)
            .ToDictionaryAsync(p => p.CourseId, p => p.CompletedAt);

        var testSessions = await _context.TestSessions
            .Where(s => s.UserId == employee.Id && s.IsCompleted)
            .OrderBy(s => s.StartedAt)
            .ToListAsync();

        const double passingScore = 70.0;

        bool allCoursesCompleted = assignedCourses.Any() &&
            assignedCourses.All(c => completedCourseIds.Contains(c.Id));

        double averageTestScore = testSessions.Any()
            ? testSessions.Average(s => s.TotalQuestions > 0
                ? (double)s.CorrectAnswers / s.TotalQuestions * 100
                : 0)
            : 0;

        bool isCompliant = allCoursesCompleted && averageTestScore >= passingScore;

        string nonCompliantReason = "";
        if (!isCompliant)
        {
            if (!assignedCourses.Any())
                nonCompliantReason = "Niciun curs atribuit.";
            else if (!allCoursesCompleted)
            {
                var remaining = assignedCourses.Count(c => !completedCourseIds.Contains(c.Id));
                if (remaining == 1)
                    nonCompliantReason = "Un curs nefinalizat.";
                else if (remaining > 1)
                    nonCompliantReason = $"{remaining} cursuri nefinalizate.";
            }
            else if (!testSessions.Any())
                nonCompliantReason = "Niciun test susținut încă.";
            else
                nonCompliantReason = $"Scorul mediu la teste este {averageTestScore:0.0}%, necesar ≥ {passingScore}%.";
        }

        ViewBag.Employee = employee;
        ViewBag.AssignedCourses = assignedCourses;
        ViewBag.CompletedCourseIds = completedCourseIds;
        ViewBag.CompletedChapterIds = completedChapterIds;
        ViewBag.ChapterProgressMap = chapterProgressMap;
        ViewBag.CourseProgressMap = courseProgressMap;
        ViewBag.TestSessions = testSessions;
        ViewBag.IsCompliant = isCompliant;
        ViewBag.NonCompliantReason = nonCompliantReason;
        ViewBag.AverageTestScore = averageTestScore;
        ViewBag.PassingScore = passingScore;
        ViewBag.ReportDate = DateTime.Now;
        ViewBag.ClinicName = admin.Clinic?.Name;

        return View();
    }
}
