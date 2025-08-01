using System.Security.Cryptography.X509Certificates;
using ItemManagement.FileUpload;
using ItemManagement.Helper;
using Microsoft.AspNetCore.Authentication.Cookies;
using StoreLocation = ItemManagement.Helper.StoreLocation;
using System.Net.Http.Headers; // Added for HttpClient headers
using Microsoft.Extensions.Configuration; // Ensure this is present if IConfiguration is used directly

var builder = WebApplication.CreateBuilder(args);

// Add session based authentication
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add cookie-based authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register FileUploadService and StoreLocation
builder.Services.AddSingleton<FileUploadService>();
builder.Services.AddTransient<StoreLocation>();

// Register a Typed HttpClient for your Backend API
builder.Services.AddHttpClient("ItemManagementApiClient", client =>
{
    // Get the BaseUrl from appsettings.json
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"]
        ?? throw new ArgumentNullException("ApiSettings:BaseUrl is not configured in appsettings.json.");
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddScoped<StoreLocation>();
builder.Services.AddHttpContextAccessor();

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

app.UseAuthentication();
app.UseAuthorization();

// Use MapControllers for attribute routing
app.MapControllers();

// Removed default conventional route map as MapControllers is now handling attribute routing.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
