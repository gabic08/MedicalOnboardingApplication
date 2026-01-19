using MedicalOnboardingApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Data
{
    public class MedicalOnboardingApplicationContext : DbContext
    {
        public MedicalOnboardingApplicationContext (DbContextOptions<MedicalOnboardingApplicationContext> options)
            : base(options)
        {
        }

        public DbSet<Course> Course { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<Test> Tests { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }

    }
}
