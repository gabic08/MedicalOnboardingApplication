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

    // GET: Clinic/Edit
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.Users
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (user == null)
            return NotFound();

        // If clinic doesn't exist yet → create empty model
        if (user.Clinic == null)
        {
            return View(new Clinic());
        }

        return View(user.Clinic);
    }

    // POST: Clinic/Edit
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
            return View(clinic);

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
        return RedirectToAction(nameof(Edit));
    }

    // GET: Clinic/Details
    public async Task<IActionResult> Details()
    {
        var user = await _userManager.Users
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (user == null)
            return NotFound();

        if (user.Clinic == null)
            return RedirectToAction(nameof(Edit));

        return View(user.Clinic);
    }

}
