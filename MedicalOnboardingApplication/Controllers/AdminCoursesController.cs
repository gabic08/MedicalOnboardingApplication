using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCoursesController : Controller
    {
        private readonly MedicalOnboardingApplicationContext _context;

        public AdminCoursesController(MedicalOnboardingApplicationContext context)
        {
            _context = context;
        }

        // GET: Courses
        public async Task<IActionResult> Index(int? employeeTypeId, string search)
        {
            var coursesQuery = _context.Courses
                .Include(c => c.CourseEmployeeTypes)
                    .ThenInclude(cet => cet.EmployeeType)
                .AsQueryable();

            if (employeeTypeId.HasValue)
            {
                coursesQuery = coursesQuery
                    .Where(c => c.CourseEmployeeTypes
                        .Any(cet => cet.EmployeeTypeId == employeeTypeId.Value));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                coursesQuery = coursesQuery.Where(c =>
                    c.Title.Contains(term) ||
                    (c.Description != null && c.Description.Contains(term)));
            }

            var courses = await coursesQuery
                .OrderBy(c => c.Order)
                .ToListAsync();

            ViewBag.EmployeeTypes = await _context.EmployeeTypes.ToListAsync();
            ViewBag.SelectedEmployeeTypeId = employeeTypeId;
            ViewBag.Search = search;

            return View(courses);
        }


        public async Task<IActionResult> Manage(int id)
        {
            var course = await _context.Courses
                .Include(c => c.CourseEmployeeTypes)
                    .ThenInclude(cet => cet.EmployeeType)
                .Include(c => c.Chapters)
                    .ThenInclude(ch => ch.Attachments)
                .Include(t => t.Questions)
                    .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
                return NotFound();

            var allEmployeeTypes = await _context.EmployeeTypes.ToListAsync();

            var vm = new CourseViewModel
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

            ViewBag.Course = course;
            return View(vm);
        }

        // GET: Courses/Create
        public async Task<IActionResult> Create()
        {
            var lastOrder = await _context.Courses
                .OrderByDescending(c => c.Order)
                .Select(c => c.Order)
                .FirstOrDefaultAsync();

            var allEmployeeTypes = await _context.EmployeeTypes.ToListAsync();

            var model = new CourseViewModel
            {
                Order = lastOrder + 1,
                EmployeeTypes = allEmployeeTypes.Select(et => new EmployeeTypeCheckbox
                {
                    Id = et.Id,
                    Name = et.Name,
                    IsSelected = false
                }).ToList()
            };

            return View(model);
        }

        // POST: Courses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CourseViewModel model)
        {
            // Normalize title
            model.Title = model.Title?.Trim();

            // Title validation
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                ModelState.AddModelError("Title", "Title is required.");
            }
            else
            {
                var exists = await _context.Courses
                    .AnyAsync(c => c.Title.ToLower() == model.Title.ToLower());

                if (exists)
                {
                    ModelState.AddModelError("Title", "A course with this title already exists.");
                }
            }

            // Reload employee types if validation fails
            if (!ModelState.IsValid)
            {
                var allEmployeeTypes = await _context.EmployeeTypes.ToListAsync();

                model.EmployeeTypes = allEmployeeTypes.Select(et => new EmployeeTypeCheckbox
                {
                    Id = et.Id,
                    Name = et.Name,
                    IsSelected = model.EmployeeTypes?.Any(e => e.Id == et.Id && e.IsSelected) == true
                }).ToList();

                return View(model);
            }

            // Get max order
            var maxOrder = await _context.Courses
                .MaxAsync(c => (int?)c.Order) ?? 0;

            // Normalize requested order
            var requestedOrder = model.Order < 1 ? 1 :
                                 model.Order > maxOrder + 1 ? maxOrder + 1 :
                                 model.Order;

            // Shift existing courses down
            var coursesToShift = await _context.Courses
                .Where(c => c.Order >= requestedOrder)
                .OrderBy(c => c.Order)
                .ToListAsync();

            foreach (var c in coursesToShift)
            {
                c.Order++;
            }

            // Create course
            var course = new Course
            {
                Title = model.Title,
                Description = model.Description,
                Order = requestedOrder
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            // Save employee type relationships
            foreach (var et in model.EmployeeTypes.Where(e => e.IsSelected))
            {
                _context.CourseEmployeeTypes.Add(new CourseEmployeeType
                {
                    CourseId = course.Id,
                    EmployeeTypeId = et.Id
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
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

            // Normalize title
            model.Title = model.Title?.Trim();

            // Title validation
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                ModelState.AddModelError("Title", "Title is required.");
            }
            else
            {
                var exists = await _context.Courses
                    .AnyAsync(c =>
                        c.Id != model.Id &&
                        c.Title.ToLower() == model.Title.ToLower());

                if (exists)
                {
                    ModelState.AddModelError("Title", "A course with this title already exists.");
                }
            }

            // Reload form if validation fails
            if (!ModelState.IsValid)
            {
                var allEmployeeTypes = await _context.EmployeeTypes.ToListAsync();

                model.EmployeeTypes = allEmployeeTypes.Select(et => new EmployeeTypeCheckbox
                {
                    Id = et.Id,
                    Name = et.Name,
                    IsSelected = model.EmployeeTypes?.Any(e => e.Id == et.Id && e.IsSelected) == true
                }).ToList();

                return View(model);
            }

            var course = await _context.Courses
                .Include(c => c.CourseEmployeeTypes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
                return NotFound();

            var oldOrder = course.Order;

            var maxOrder = await _context.Courses
                .Where(c => c.Id != course.Id)
                .MaxAsync(c => (int?)c.Order) ?? 0;

            // Normalize requested order
            var requestedOrder = model.Order < 1 ? 1 :
                                 model.Order > maxOrder + 1 ? maxOrder + 1 :
                                 model.Order;

            // Shift orders
            if (requestedOrder < oldOrder)
            {
                var coursesToShift = await _context.Courses
                    .Where(c => c.Id != course.Id &&
                                c.Order >= requestedOrder &&
                                c.Order < oldOrder)
                    .ToListAsync();

                foreach (var c in coursesToShift)
                {
                    c.Order++;
                }
            }
            else if (requestedOrder > oldOrder)
            {
                var coursesToShift = await _context.Courses
                    .Where(c => c.Id != course.Id &&
                                c.Order > oldOrder &&
                                c.Order <= requestedOrder)
                    .ToListAsync();

                foreach (var c in coursesToShift)
                {
                    c.Order--;
                }
            }

            // Update course
            course.Title = model.Title;
            course.Description = model.Description;
            course.Order = requestedOrder;

            // Update employee type relationships
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
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
                return RedirectToAction(nameof(Index));

            var deletedOrder = course.Order;

            // Delete course
            _context.Courses.Remove(course);

            // Shift everything above it up
            var coursesToShift = await _context.Courses
                .Where(c => c.Order > deletedOrder)
                .OrderBy(c => c.Order)
                .ToListAsync();

            foreach (var c in coursesToShift)
            {
                c.Order--;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
