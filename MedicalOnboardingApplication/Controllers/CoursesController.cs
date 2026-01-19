using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Controllers
{
    public class CoursesController : Controller
    {
        private readonly MedicalOnboardingApplicationContext _context;

        public CoursesController(MedicalOnboardingApplicationContext context)
        {
            _context = context;
        }

        // GET: Courses
        public async Task<IActionResult> Index(int? employeeTypeId)
        {
            var coursesQuery = _context.Courses
                .OrderBy(c => c.Order)
                .Include(c => c.CourseEmployeeTypes)
                .ThenInclude(cet => cet.EmployeeType)
                .AsQueryable();

            if (employeeTypeId.HasValue)
            {
                coursesQuery = coursesQuery
                    .Where(c => c.CourseEmployeeTypes
                        .Any(cet => cet.EmployeeTypeId == employeeTypeId.Value));
            }

            var courses = await coursesQuery.ToListAsync();

            // Employee types for the filter dropdown
            ViewBag.EmployeeTypes = await _context.EmployeeTypes.ToListAsync();
            ViewBag.SelectedEmployeeTypeId = employeeTypeId;

            return View(courses);
        }

        // GET: Courses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.Chapters)
                .Include(c => c.Tests)
                    .ThenInclude(t => t.Questions)
                        .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // GET: Courses/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Courses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Order")] Course course)
        {
            if (ModelState.IsValid)
            {
                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Courses", new { id = course.Id });
            }
            return View(course);
        }

        // GET: Courses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var course = await _context.Courses
                .Include(c => c.CourseEmployeeTypes)
                .ThenInclude(cet => cet.EmployeeType)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
                return NotFound();

            // Map to CourseViewModel
            var allEmployeeTypes = await _context.EmployeeTypes.ToListAsync();

            var viewModel = new CourseViewModel
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                Order = course.Order,
                EmployeeTypes = allEmployeeTypes.Select(et => new EmployeeTypeCheckbox
                {
                    Id = et.Id,
                    Name = et.Name,
                    IsSelected = course.CourseEmployeeTypes.Any(cet => cet.EmployeeTypeId == et.Id)
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: Courses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CourseViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var course = await _context.Courses
                .Include(c => c.CourseEmployeeTypes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
                return NotFound();

            // Update basic properties
            course.Title = model.Title;
            course.Description = model.Description;
            course.Order = model.Order;

            // Update employee type associations
            course.CourseEmployeeTypes.Clear();

            foreach (var et in model.EmployeeTypes.Where(e => e.IsSelected))
            {
                course.CourseEmployeeTypes.Add(new CourseEmployeeType
                {
                    CourseId = course.Id,
                    EmployeeTypeId = et.Id
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Courses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                _context.Courses.Remove(course);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }
    }
}
