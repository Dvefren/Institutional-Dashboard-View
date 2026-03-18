using Microsoft.AspNetCore.Authentication.Cookies;
using UTTN.Dashboard.Data;
using UTTN.Dashboard.Services.Interfaces;
using UTTN.Dashboard.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// ─── Services ────────────────────────────────────
builder.Services.AddControllersWithViews();


// Dapper context (singleton — it's just a connection factory)
builder.Services.AddSingleton<DapperContext>();

// Business services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IManagementService, ManagementService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.Name = "UTTN.Auth";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// ─── Middleware pipeline ─────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();