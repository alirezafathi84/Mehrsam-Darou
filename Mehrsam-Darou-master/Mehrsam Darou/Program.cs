using Mehrsam_Darou.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Register DbContext
builder.Services.AddDbContext<DarouAppContext>(options =>
    options.UseSqlServer("Server=DESKTOP-PL06GAP;Database=DarouApp;Trusted_Connection=True;TrustServerCertificate=True;"));

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
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    // Add specific route for actions that need ID parameter
    endpoints.MapControllerRoute(
        name: "withId",
        pattern: "{controller}/{action}/{id?}");

    // Default route
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Dashboard}/{action=Dashboard}");

    endpoints.MapHub<ChatHub>("/chatHub");
});

app.Run();

