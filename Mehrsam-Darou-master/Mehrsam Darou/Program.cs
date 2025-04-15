using Mehrsam_Darou.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
// Register DbContext
builder.Services.AddDbContext<DarouAppContext>(options =>
    options.UseSqlServer("Server=(localdb)\\localDB;Database=DarouApp;Trusted_Connection=True;TrustServerCertificate=True;"));

// Add SignalR
builder.Services.AddSignalR();

// Add session services
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();


app.UseSession();
app.UseRouting();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=User}/{action=UserList}/{id?}");
    endpoints.MapHub<ChatHub>("/chatHub");
});

app.Run();