using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalOnboardingApplication.Controllers;

[Authorize(Roles = "Employee")]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}