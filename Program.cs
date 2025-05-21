using junimo_v3.Data;
using junimo_v3.Models;
using junimo_v3.Repositories;
using junimo_v3.Repositories.Interfaces;
using junimo_v3.Services;
using junimo_v3.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// database
builder.Services.AddDbContext<RepositoryContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

//auth
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<RepositoryContext>()
    .AddDefaultTokenProviders();


// Add services to the container.
builder.Services.AddControllersWithViews();

// my services

builder.Services.AddScoped<IGameRepository,     GameRepository>();
builder.Services.AddScoped<IGameService,        GameService>();
builder.Services.AddScoped<ITicketService,      TicketService>();
builder.Services.AddScoped<IGameGenreV2Service, GameGenreV2Service>();
builder.Services.AddScoped<IOrderService,       OrderService>();
builder.Services.AddScoped<IFriendshipService,  FriendshipService>();
builder.Services.AddScoped<IUserService,        UserService>();
builder.Services.AddScoped<IReviewService,      ReviewService>();



builder.Services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();

// auth config
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("User", policy => policy.RequireRole("User"));
});
    
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "Admin", "Organizer", "User" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}


app.Run();
