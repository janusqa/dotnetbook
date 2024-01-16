using Bookstore.DataAccess.Data;
using BookStore.DataAccess.Repository;
using BookStore.DataAccess.UnitOfWork.IUnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Bookstore.Utility;
using Bookstore.Models.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();  // need to add this for Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// This was added by Identity scaffolding with the wrong context. We needed to change it back to our ApplicationDBContext
// We also needed to delete any Data Folders with DbContext that Identity would have created.
// For example one was found in the Areas/Identity/Data. That entire folder was deleted.
// builder.Services.AddDefaultIdentity<ApplicationUser>().AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
// IF YOU NEED ONLY CONFIRMED EMAILS of users to be able to sign in comment out the line above and uncomment the line below.
//builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
//builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>();

// The service below makes sure that when user tries to access resource for which they are not allow that they are re-directed 
// correctly to appropriate page (e.g access denied or login page)
// NOTE THIS MUST BE ADDED AFTER AddIdentity SERVICE!!!
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = @"/Identity/Account/Login";
    options.LogoutPath = @"/Identity/Account/Logout";
    options.AccessDeniedPath = @"/Identity/Account/AccessDenied";
});

// enable sessions
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(100);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEmailSender, EmailSender>();

// enable facebook logins
builder.Services.AddAuthentication().AddFacebook(options =>
{
    options.AppId = builder.Configuration.GetValue<string>("FacebookAppId") ?? "";
    options.AppSecret = builder.Configuration.GetValue<string>("FacebookAppSecret") ?? "";
}).AddGoogle(options =>
{
    options.ClientId = builder.Configuration.GetValue<string>("GoogleAppId") ?? "";
    options.ClientSecret = builder.Configuration.GetValue<string>("GoogleAppSecret") ?? "";
});

// ***BEGIN CUSTOM CONFIG FOR SECRETS
// API KEYS STORE IN SECRETS (dotnet user-secrets init)
// Stripe Config
Stripe.StripeConfiguration.ApiKey = builder.Configuration.GetValue<string>("StripeSecretKey");
// ***END CUSTOM CONFIT FOR SECRETS

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// we need to add "UseAuthentication before UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

// enable sessions
app.UseSession();

app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

app.Run();
