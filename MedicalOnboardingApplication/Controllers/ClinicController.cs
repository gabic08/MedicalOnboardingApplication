using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Controllers;

[Authorize(Roles = "Admin")]
public class ClinicController : Controller
{
    private readonly MedicalOnboardingApplicationContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClinicController(
        MedicalOnboardingApplicationContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // ===============================
    // GET: Clinic/Details
    // ===============================
    public async Task<IActionResult> Details()
    {
        var user = await _userManager.Users
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (user == null)
            return NotFound();

        // If clinic doesn't exist yet, send empty model
        return View(user.Clinic ?? new Clinic());
    }

    // ===============================
    // POST: Clinic/Edit (Save)
    // ===============================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Clinic clinic)
    {
        var user = await _userManager.Users
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (user == null)
            return NotFound();

        if (!ModelState.IsValid)
            return View("Details", clinic);

        if (user.ClinicId == null)
        {
            // CREATE new clinic
            _context.Clinics.Add(clinic);
            await _context.SaveChangesAsync();

            user.ClinicId = clinic.Id;
            await _userManager.UpdateAsync(user);
        }
        else
        {
            // UPDATE existing clinic
            var existingClinic = await _context.Clinics
                .FirstOrDefaultAsync(c => c.Id == user.ClinicId);

            if (existingClinic == null)
                return NotFound();

            existingClinic.Name = clinic.Name;
            existingClinic.Address = clinic.Address;
            existingClinic.Phone = clinic.Phone;

            await _context.SaveChangesAsync();
        }

        TempData["Success"] = "Clinic information saved successfully.";

        // Redirect back to Details (main page)
        return RedirectToAction(nameof(Details));
    }
}
