using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Filters;

public class WorkingHoursAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var userManager = httpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = httpContext.RequestServices.GetRequiredService<MedicalOnboardingApplicationContext>();

        var user = await userManager.GetUserAsync(httpContext.User);

        if (user == null || !await userManager.IsInRoleAsync(user, "Employee"))
        {
            await next();
            return;
        }

        var now = DateTime.Now;
        var currentDay = now.DayOfWeek;
        var currentTime = TimeOnly.FromDateTime(now);

        var todayShifts = await dbContext.WorkSchedules
            .Where(s => s.UserId == user.Id && s.Day == currentDay)
            .ToListAsync();

        bool isWorkingNow = todayShifts.Any(s =>
            currentTime >= s.StartTime && currentTime <= s.EndTime);

        if (!isWorkingNow)
        {
            context.Result = new RedirectToActionResult("Index", "Dashboard", null);
            return;
        }

        await next();
    }
}