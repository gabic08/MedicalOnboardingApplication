using MedicalOnboardingApplication;
using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Middleware;
using MedicalOnboardingApplication.Models;
using MedicalOnboardingApplication.Services;
using MedicalOnboardingApplication.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localtest.me:5000");

// Database
builder.Services.AddDbContext<MedicalOnboardingApplicationContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedAccount = true;

    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<MedicalOnboardingApplicationContext>()
.AddDefaultTokenProviders();

var baseDomain = builder.Configuration["AppSettings:BaseDomain"];

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Domain = $".{baseDomain}";
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
});

var app = builder.Build();

// Auto-run migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MedicalOnboardingApplicationContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseMiddleware<SubdomainMiddleware>();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Accept-Ranges"] = "bytes";
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();
