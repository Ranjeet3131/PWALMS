using Microsoft.EntityFrameworkCore;
using PWALMS.Data;
using PWALMS.Models;
using PWALMS.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add SQLite Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Add Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<QuizService>();
builder.Services.AddScoped<ExportService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Add Session middleware
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// ================================================
// DATABASE SETUP AND SEEDING
// ================================================
try
{
    Console.WriteLine("🚀 Starting PWA LMS Application...");

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create database if it doesn't exist
        Console.WriteLine("📁 Creating database...");
        db.Database.EnsureCreated();
        Console.WriteLine("✅ Database ready!");

        // Seed initial data if database is empty
        if (!db.Roles.Any())
        {
            Console.WriteLine("🌱 Seeding initial data...");

            // Add Roles
            var roles = new List<Role>
            {
                new Role { RoleName = "Admin", Description = "Full system access" },
                new Role { RoleName = "Uploader", Description = "Can create quizzes and view scores" },
                new Role { RoleName = "QuizTaker", Description = "Can only take quizzes" }
            };
            db.Roles.AddRange(roles);
            db.SaveChanges();
            Console.WriteLine("✅ Added 3 roles");

            // Add Departments
            var departments = new List<Department>
            {
                new Department { DepartmentName = "Bloodbank", DepartmentCode = "BB" },
                new Department { DepartmentName = "Administration", DepartmentCode = "ADMIN" },
                new Department { DepartmentName = "IT", DepartmentCode = "IT" },
                new Department { DepartmentName = "Accounts", DepartmentCode = "ACC" },
                new Department { DepartmentName = "Thalassemia Care", DepartmentCode = "TC" }
            };
            db.Departments.AddRange(departments);
            db.SaveChanges();
            Console.WriteLine("✅ Added 5 departments");

            // Add Admin User
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                FullName = "System Administrator",
                Email = "admin@pwa.org",
                RoleID = roles[0].RoleID,
                DepartmentID = departments[1].DepartmentID, // Administration
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            db.Users.Add(adminUser);
            db.SaveChanges();
            Console.WriteLine("✅ Added admin user");
            Console.WriteLine("   👤 Username: admin");
            Console.WriteLine("   🔐 Password: admin123");

            Console.WriteLine("🎉 Database seeding complete!");
        }
        else
        {
            Console.WriteLine("✅ Database already has data.");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ ERROR: {ex.Message}");
}

Console.WriteLine("🚀 Application ready!");
app.Run();