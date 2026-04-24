using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using UnisexClothes.Models;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------
// 1. Add services
// -------------------------------------------

// Add MVC (Controllers + Views)
builder.Services.AddControllersWithViews();

// Add Session (Admin login)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Allow large file uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue;
});

// Add DbContext - nhớ thay connection string vào ↓↓↓↓↓↓↓↓↓↓↓↓↓↓
builder.Services.AddDbContext<UniStyleDbContext>(options =>
    options.UseSqlServer("Server=LAPTOP-FMT7NVUF;Database=UniStyleDB;Trusted_Connection=True;TrustServerCertificate=True")
);

// -------------------------------------------
// 2. Build app
// -------------------------------------------
var app = builder.Build();

// -------------------------------------------
// 3. Configure HTTP pipeline
// -------------------------------------------

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();          // Enable Session
app.UseAuthorization();    // Authorization

// Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// -------------------------------------------
// 4. Run app
// -------------------------------------------
app.Run();
