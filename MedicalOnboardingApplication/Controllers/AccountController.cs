using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Models;
using MedicalOnboardingApplication.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace MedicalOnboardingApplication.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MedicalOnboardingApplicationContext _context;
        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            MedicalOnboardingApplicationContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return LocalRedirect("/");
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);
            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Încearcare de autentificare invalidă.");
                return View(vm);
            }
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName,
                vm.Password,
                isPersistent: false,
                lockoutOnFailure: true);
            if (result.Succeeded)
            {
                return LocalRedirect("/");
            }
            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Cont blocat. Încearcă din nou mai târziu.");
                return View(vm);
            }
            ModelState.AddModelError("", "Încearcare de autentificare invalidă.");
            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
        [HttpGet]
        public IActionResult RegisterClinicAdmin(string returnUrl = null)
        {
            return View(new RegisterAdminViewModel
            {
                ReturnUrl = returnUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterClinicAdmin(RegisterAdminViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var existingUser = await _userManager.FindByEmailAsync(vm.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "Un cont cu această adresă de email există deja.");
                return View(vm);
            }

            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, vm.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(vm);
            }

            await _userManager.AddToRoleAsync(user, "Admin");
            return LocalRedirect(vm.ReturnUrl ?? Url.Action("Login", "Account")!);
        }
    }
}
