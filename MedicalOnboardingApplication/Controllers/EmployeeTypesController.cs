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

        var types = await query.OrderBy(e => e.Name).ToListAsync();

        // Build a set of IDs that are in use (by users or courses)
        var allIds = types.Select(t => t.Id).ToList();

        var usedByUsers = await _context.Users
            .Where(u => u.EmployeeTypeId.HasValue && allIds.Contains(u.EmployeeTypeId.Value))
            .Select(u => u.EmployeeTypeId.Value)
            .Distinct()
            .ToListAsync();

        var usedByCourses = await _context.CourseEmployeeTypes
            .Where(c => allIds.Contains(c.EmployeeTypeId))
            .Select(c => c.EmployeeTypeId)
            .Distinct()
            .ToListAsync();

        var usedIds = usedByUsers.Union(usedByCourses).ToHashSet();

        ViewBag.Search = search;
        ViewBag.EditId = editId;
        ViewBag.UsedIds = usedIds;

        return View(types);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Numele este obligatoriu.";
            return RedirectToAction(nameof(Index));
        }

        name = name.Trim();

        var exists = await _context.EmployeeTypes
            .AnyAsync(e => e.Name.ToLower() == name.ToLower());

        if (exists)
        {
            TempData["Error"] = "Acest tip de angajat există deja.";
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
            TempData["Error"] = "Numele este obligatoriu.";
            return RedirectToAction(nameof(Index), new { editId = id });
        }

        name = name.Trim();

        var exists = await _context.EmployeeTypes
            .AnyAsync(e => e.Id != id && e.Name.ToLower() == name.ToLower());

        if (exists)
        {
            TempData["Error"] = "Acest tip de angajat există deja.";
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
        if (type == null) return NotFound();

        var usedByUsers = await _context.Users
            .AnyAsync(u => u.EmployeeTypeId == id);

        var usedByCourses = await _context.CourseEmployeeTypes
            .AnyAsync(c => c.EmployeeTypeId == id);

        if (usedByUsers || usedByCourses)
        {
            var parts = new List<string>();
            if (usedByUsers) parts.Add("angajați");
            if (usedByCourses) parts.Add("cursuri");

            TempData["Error"] = $"Tipul '{type.Name}' nu poate fi șters deoarece este folosit de {string.Join(" și ", parts)}.";
            return RedirectToAction(nameof(Index));
        }

        _context.EmployeeTypes.Remove(type);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
