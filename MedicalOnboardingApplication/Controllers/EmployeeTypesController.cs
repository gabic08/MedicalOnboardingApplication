using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Admin")]
public class EmployeeTypesController : Controller
{
    private readonly MedicalOnboardingApplicationContext _context;

    public EmployeeTypesController(MedicalOnboardingApplicationContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string search, int? editId = null)
    {
        var query = _context.EmployeeTypes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.Name.Contains(term));
        }

        ViewBag.Search = search;
        ViewBag.EditId = editId;

        return View(await query.OrderBy(e => e.Name).ToListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Name is required.";
            return RedirectToAction(nameof(Index));
        }

        name = name.Trim();

        var exists = await _context.EmployeeTypes
            .AnyAsync(e => e.Name.ToLower() == name.ToLower());

        if (exists)
        {
            TempData["Error"] = "This employee type already exists.";
            return RedirectToAction(nameof(Index));
        }

        _context.EmployeeTypes.Add(new EmployeeType { Name = name });
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Name is required.";
            return RedirectToAction(nameof(Index), new { editId = id });
        }

        name = name.Trim();

        var exists = await _context.EmployeeTypes
            .AnyAsync(e => e.Id != id && e.Name.ToLower() == name.ToLower());

        if (exists)
        {
            TempData["Error"] = "This employee type already exists.";
            return RedirectToAction(nameof(Index), new { editId = id });
        }

        var type = await _context.EmployeeTypes.FindAsync(id);
        if (type == null) return NotFound();

        type.Name = name;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var type = await _context.EmployeeTypes.FindAsync(id);
        if (type != null)
        {
            _context.EmployeeTypes.Remove(type);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
