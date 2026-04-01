//using DataBridge.Data;
//using DataBridge.Middleware;
//using DataBridge.Repositories.EF;
//using DataBridge.Repositories.Interfaces;
//using DataBridge.Services;
//using Microsoft.EntityFrameworkCore;

//var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddControllersWithViews();

//// EF Core
//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//// Session
//builder.Services.AddSession(options =>
//{
//    options.IdleTimeout = TimeSpan.FromHours(8);
//    options.Cookie.HttpOnly = true;
//    options.Cookie.IsEssential = true;
//    options.Cookie.Name = ".DataBridge.Session";
//});
//builder.Services.AddHttpContextAccessor();

//// Repositories & Services
//builder.Services.AddScoped<IUserRepository, UserRepository>();
//builder.Services.AddScoped<AuthService>();
//builder.Services.AddScoped<DataBridge.Services.UserService>();

//builder.Services.AddSession(options =>
//{
//    options.IdleTimeout = TimeSpan.FromHours(8);
//    options.Cookie.HttpOnly = true;
//    options.Cookie.IsEssential = true;
//    options.Cookie.Name = ".DataBridge.Session";
//});

//var app = builder.Build();

//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseRouting();
//app.UseSession();
//app.UseMiddleware<AuthMiddleware>();
//app.UseAuthorization();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Auth}/{action=Index}/{id?}");

//// Fallback: /Auth ? /Auth/Login (karena Auth tidak punya Index)
//app.MapControllerRoute(
//    name: "auth_login",
//    pattern: "Auth",
//    defaults: new { controller = "Auth", action = "Login" });

//app.Run();

using DataBridge.Data;
using DataBridge.Middleware;
using DataBridge.Repositories.EF;
using DataBridge.Repositories.Interfaces;
using DataBridge.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".DataBridge.Session";
});
builder.Services.AddHttpContextAccessor();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISourceRepository, SourceRepository>();
builder.Services.AddScoped<IMirrorJobRepository, MirrorJobRepository>();
builder.Services.AddScoped<IJobHistoryRepository, JobHistoryRepository>();

// Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<SourceService>();
builder.Services.AddScoped<MirrorJobService>();
builder.Services.AddScoped<ScheduleService>();
builder.Services.AddScoped<JobHistoryService>();
builder.Services.AddScoped<EmailConfigService>();
builder.Services.AddScoped<DashboardService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseMiddleware<AuthMiddleware>();
app.UseAuthorization();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Index}/{id?}");

// Fallback: /Auth ? /Auth/Login (karena Auth tidak punya Index)
app.MapControllerRoute(
    name: "auth_login",
    pattern: "Auth",
    defaults: new { controller = "Auth", action = "Login" });


app.Run();