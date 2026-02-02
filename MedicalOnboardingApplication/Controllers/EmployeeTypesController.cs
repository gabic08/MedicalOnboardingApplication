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

    public async Task<IActionResult> Index(string search)
    {
        var query = _context.EmployeeTypes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.Name.Contains(term));
        }

        var types = await query
            .OrderBy(e => e.Name)
            .ToListAsync();

        ViewBag.Search = search;

        return View(types);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeType employeeType)
    {
        if (string.IsNullOrWhiteSpace(employeeType.Name))
        {
            ModelState.AddModelError("Name", "Name is required.");
        }
        else
        {
            var exists = await _context.EmployeeTypes
                .AnyAsync(e => e.Name.ToLower() == employeeType.Name.Trim().ToLower());

            if (exists)
            {
                ModelState.AddModelError("Name", "This employee type already exists.");
            }
        }

        if (!ModelState.IsValid)
            return View(employeeType);

        employeeType.Name = employeeType.Name.Trim();

        _context.EmployeeTypes.Add(employeeType);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
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
        if (id != employeeType.Id)
            return NotFound();

        if (string.IsNullOrWhiteSpace(employeeType.Name))
        {
            ModelState.AddModelError("Name", "Name is required.");
        }
        else
        {
            var exists = await _context.EmployeeTypes
                .AnyAsync(e =>
                    e.Id != employeeType.Id &&
                    e.Name.ToLower() == employeeType.Name.Trim().ToLower());

            if (exists)
            {
                ModelState.AddModelError("Name", "This employee type already exists.");
            }
        }

        if (!ModelState.IsValid)
            return View(employeeType);

        employeeType.Name = employeeType.Name.Trim();

        _context.Update(employeeType);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
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
