using MedicalOnboardingApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Data
{
    public class MedicalOnboardingApplicationContext : DbContext
    {
        public MedicalOnboardingApplicationContext(DbContextOptions<MedicalOnboardingApplicationContext> options)
            : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<Test> Tests { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<EmployeeType> EmployeeTypes { get; set; }
        public DbSet<CourseEmployeeType> CourseEmployeeTypes { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure many-to-many
            modelBuilder.Entity<CourseEmployeeType>()
                .HasKey(cet => new { cet.CourseId, cet.EmployeeTypeId });

            modelBuilder.Entity<CourseEmployeeType>()
                .HasOne(cet => cet.Course)
                .WithMany(c => c.CourseEmployeeTypes)
                .HasForeignKey(cet => cet.CourseId);

            modelBuilder.Entity<CourseEmployeeType>()
                .HasOne(cet => cet.EmployeeType)
                .WithMany(et => et.CourseEmployeeTypes)
                .HasForeignKey(cet => cet.EmployeeTypeId);
        }

    }
}
