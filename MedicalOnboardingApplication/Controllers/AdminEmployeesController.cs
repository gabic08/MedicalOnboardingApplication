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

    // ============================
    // INDEX
    // ============================
    public async Task<IActionResult> Index()
    {
        var currentAdmin = await _userManager.Users
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        var employees = await _userManager.Users
             .Include(u => u.EmployeeType)
             .Where(u => u.ClinicId == currentAdmin.ClinicId
                      && u.UserRoles.Any(ur => ur.Role.Name != "Admin"))
             .ToListAsync();

        return View(employees);
    }

    // ============================
    // CREATE
    // ============================
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

        var result = await _userManager.CreateAsync(
            model,
            "P@ssw0rd"   // default password
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

    // ============================
    // EDIT
    // ============================
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

    // ============================
    // DELETE
    // ============================
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.Users
            .Include(u => u.EmployeeType)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound();

        return View(user);
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
}
