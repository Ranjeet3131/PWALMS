using Microsoft.EntityFrameworkCore;
using PWALMS.Models;

namespace PWALMS.Data
{
    public static class SeedData
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Ensure database is created
            context.Database.EnsureCreated();

            // Check if we already have data
            if (context.Users.Any())
            {
                return; // DB has been seeded
            }

            // Add Roles
            var roles = new Role[]
            {
                new Role { RoleName = "Admin", Description = "Full system access" },
                new Role { RoleName = "Uploader", Description = "Can create quizzes and view scores" },
                new Role { RoleName = "QuizTaker", Description = "Can only take quizzes" }
            };
            context.Roles.AddRange(roles);
            context.SaveChanges();

            // Add Departments
            var departments = new Department[]
            {
                new Department { DepartmentName = "Bloodbank", DepartmentCode = "BB" },
                new Department { DepartmentName = "Administration", DepartmentCode = "ADMIN" },
                new Department { DepartmentName = "IT", DepartmentCode = "IT" },
                new Department { DepartmentName = "Accounts", DepartmentCode = "ACC" },
                new Department { DepartmentName = "Thalassemia Care", DepartmentCode = "TC" }
            };
            context.Departments.AddRange(departments);
            context.SaveChanges();

            // Add Admin User (password: admin123)
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                FullName = "System Administrator",
                Email = "admin@pwa.org",
                RoleID = roles[0].RoleID, // Admin role
                DepartmentID = departments[1].DepartmentID, // Administration department
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            context.Users.Add(adminUser);
            context.SaveChanges();

            Console.WriteLine("Database seeded successfully!");
        }
    }
}