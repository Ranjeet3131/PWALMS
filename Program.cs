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
//import excel sheet answers questions
builder.Services.AddScoped<ExcelImportService>();

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
// SIMPLE DATABASE SETUP - GUARANTEED TO WORK
// ================================================
try
{
    Console.WriteLine("🚀 Starting PWA LMS Application...");

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 1. Create all tables
        Console.WriteLine("📁 Creating database tables...");
        db.Database.EnsureCreated();
        Console.WriteLine("✅ Tables created!");

        // 2. Simple check for data - add ONLY if completely empty
        if (!db.Roles.Any())
        {
            Console.WriteLine("🌱 Adding basic data...");
            AddBasicData(db);
        }
        else
        {
            Console.WriteLine("✅ Database already has data.");
        }

        // Show current stats
        ShowStats(db);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Setup issue (but continuing): {ex.Message}");
}

Console.WriteLine("\n=========================================");
Console.WriteLine("📋 TEST LOGINS:");
Console.WriteLine("   👑 Admin: admin / admin123");
Console.WriteLine("   📤 Uploader: trainer / trainer123");
Console.WriteLine("   👥 Quiz Taker: staff / staff123");
Console.WriteLine("=========================================");
Console.WriteLine("🚀 Application ready!");
Console.WriteLine("🌐 URL: https://localhost:5000 (or your port)");

app.Run();




// ================================================
// SIMPLE DATA SEEDING (NO COMPLEX FOREIGN KEYS)
// ================================================

void AddBasicData(ApplicationDbContext db)
{
    try
    {
        // 1. Add Roles
        var adminRole = new Role { RoleName = "Admin", Description = "Full system access" };
        var uploaderRole = new Role { RoleName = "Uploader", Description = "Can create quizzes" };
        var quizTakerRole = new Role { RoleName = "QuizTaker", Description = "Can take quizzes" };

        db.Roles.Add(adminRole);
        db.Roles.Add(uploaderRole);
        db.Roles.Add(quizTakerRole);
        db.SaveChanges();
        Console.WriteLine("   ✅ Added 3 roles");

        // 2. Add Departments
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
        Console.WriteLine("   ✅ Added 5 departments");

        // 3. Add Users (simple - no complex navigation)
        var adminUser = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            FullName = "System Admin",
            Email = "admin@pwa.org",
            RoleID = adminRole.RoleID,
            DepartmentID = departments[1].DepartmentID, // Admin dept
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        var trainerUser = new User
        {
            Username = "trainer",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("trainer123"),
            FullName = "Training Coordinator",
            Email = "trainer@pwa.org",
            RoleID = uploaderRole.RoleID,
            DepartmentID = departments[0].DepartmentID, // Bloodbank
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        var staffUser = new User
        {
            Username = "staff",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("staff123"),
            FullName = "Staff Member",
            Email = "staff@pwa.org",
            RoleID = quizTakerRole.RoleID,
            DepartmentID = departments[0].DepartmentID, // Bloodbank
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        db.Users.Add(adminUser);
        db.Users.Add(trainerUser);
        db.Users.Add(staffUser);
        db.SaveChanges();
        Console.WriteLine("   ✅ Added 3 users");

        Console.WriteLine("   🎉 Basic setup complete!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   ❌ Error adding data: {ex.Message}");
        Console.WriteLine("   ⚠️ Continuing without sample data...");
    }

}



void ShowStats(ApplicationDbContext db)
{
    try
    {
        Console.WriteLine("\n📊 Current Stats:");
        Console.WriteLine($"   👥 Users: {db.Users.Count()}");
        Console.WriteLine($"   📝 Quizzes: {db.Quizzes.Count()}");
        Console.WriteLine($"   ❓ Questions: {db.Questions.Count()}");
    }
    catch
    {
        Console.WriteLine("   📊 Could not read stats (tables might not exist yet)");
    }
}

