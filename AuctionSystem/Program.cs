using AuctionSystem.Hubs;
using AuctionSystem.Models;
using AuctionSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// EF Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
	options.Password.RequireDigit = false;
	options.Password.RequiredLength = 6;
	options.Password.RequireNonAlphanumeric = false;
	options.Password.RequireUppercase = false;
	options.Password.RequireLowercase = false;
	options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
	options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cookie
builder.Services.ConfigureApplicationCookie(options =>
{
	options.LoginPath = "/Account/Login";
	options.LogoutPath = "/Account/Logout";
	options.AccessDeniedPath = "/Account/Login";
});

// Memory cache (dùng cho rate limiting)
builder.Services.AddMemoryCache();

// SignalR
builder.Services.AddSignalR();

// Services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IProxyBidService, ProxyBidService>();
builder.Services.AddHostedService<AuctionBackgroundService>();

var app = builder.Build();

// Seed Admin role + tài khoản admin
using (var scope = app.Services.CreateScope())
{
	var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
	var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

	if (!await roleManager.RoleExistsAsync("Admin"))
		await roleManager.CreateAsync(new IdentityRole("Admin"));

	// Tạo tài khoản admin mặc định nếu chưa có
	var adminEmail = "admin@auctionhub.vn";
	var adminUser = await userManager.FindByEmailAsync(adminEmail);
	if (adminUser == null)
	{
		adminUser = new ApplicationUser
		{
			UserName = adminEmail,
			Email = adminEmail,
			FullName = "Administrator",
			EmailConfirmed = true
		};
		await userManager.CreateAsync(adminUser, "Admin@123456");
		await userManager.AddToRoleAsync(adminUser, "Admin");
	}
}

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

app.MapHub<AuctionHub>("/auctionHub");

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
