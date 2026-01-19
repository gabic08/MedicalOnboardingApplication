using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Controllers;

public class EmployeeTypesController : Controller
{
    private readonly MedicalOnboardingApplicationContext _context;

    public EmployeeTypesController(MedicalOnboardingApplicationContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.EmployeeTypes.ToListAsync());
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeType employeeType)
    {
        if (ModelState.IsValid)
        {
            _context.EmployeeTypes.Add(employeeType);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(employeeType);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var employeeType = await _context.EmployeeTypes.FindAsync(id);
        if (employeeType == null) return NotFound();
        return View(employeeType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmployeeType employeeType)
    {
        if (id != employeeType.Id) return NotFound();

        if (ModelState.IsValid)
        {
            _context.Update(employeeType);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(employeeType);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var employeeType = await _context.EmployeeTypes.FindAsync(id);
        if (employeeType == null) return NotFound();
        return View(employeeType);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var employeeType = await _context.EmployeeTypes.FindAsync(id);
        if (employeeType != null)
        {
            _context.EmployeeTypes.Remove(employeeType);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
