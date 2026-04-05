using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartTaskManagementSystem.Data;
using SmartTaskManagementSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// ======================
// DATABASE CONFIGURATION
// ======================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Development exception filter (DB hatalarý için)
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ======================
// SERVICES
// ======================
builder.Services.AddScoped<AuditLogService>();

// ======================
// IDENTITY CONFIGURATION
// ======================
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    // Optional: password rules (istersen açarsýn)
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// ======================
// MVC + RAZOR
// ======================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// ======================
// PIPELINE
// ======================
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Identity middleware
app.UseAuthentication();
app.UseAuthorization();

// ======================
// ROUTES
// ======================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();