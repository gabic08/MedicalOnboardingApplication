using MedicalOnboardingApplication.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Data
{
    public class MedicalOnboardingApplicationContext : IdentityDbContext<ApplicationUser>
    {
        public MedicalOnboardingApplicationContext(DbContextOptions<MedicalOnboardingApplicationContext> options)
            : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<EmployeeType> EmployeeTypes { get; set; }
        public DbSet<CourseEmployeeType> CourseEmployeeTypes { get; set; }
        public DbSet<ChapterAttachment> ChapterAttachments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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


            // Delete cascade for Questions and Answers
            modelBuilder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .OnDelete(DeleteBehavior.Cascade);

        }

    }
}
