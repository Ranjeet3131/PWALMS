using Microsoft.EntityFrameworkCore;
using PWALMS.Models;

namespace PWALMS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Option> Options { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }
        public DbSet<UserAnswer> UserAnswers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // For SQLite, convert decimal to double
            modelBuilder.Entity<Quiz>()
                .Property(q => q.TotalMarks)
                .HasConversion<double>();

            modelBuilder.Entity<Question>()
                .Property(q => q.Marks)
                .HasConversion<double>();

            modelBuilder.Entity<QuizAttempt>()
                .Property(q => q.Score)
                .HasConversion<double>();

            modelBuilder.Entity<QuizAttempt>()
                .Property(q => q.MaxScore)
                .HasConversion<double>();

            modelBuilder.Entity<QuizAttempt>()
                .Property(q => q.Percentage)
                .HasConversion<double>();

            modelBuilder.Entity<UserAnswer>()
                .Property(u => u.MarksObtained)
                .HasConversion<double>();
        }
    }
}