using System.Text;
using System.Security.Claims;
using ItemManagement.Model;
using ItemManagement.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ItemManagement.Interface;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// 1️⃣  MVC Controllers
// ---------------------------------------------------------------------
builder.Services.AddControllers();

// ---------------------------------------------------------------------
// 2️⃣  Swagger + JWT support (so Postman ki zaroorat bhi nahin padhe)
// ---------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ItemManagement API",
        Version = "v1"
    });
    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter **Bearer &lt;token&gt;**",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    c.AddSecurityDefinition("Bearer", bearerScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { bearerScheme, Array.Empty<string>() }
    });
});

// ---------------------------------------------------------------------
// 3️⃣  Application Services
// ---------------------------------------------------------------------
builder.Services.AddScoped<IItemService, ItemService>(); // UNCOMMENT THIS LINE!
builder.Services.AddScoped<JwtTokenServices>();
builder.Services.AddSingleton<JwtTokenServices>();
// or, in Startup.cs:
// services.AddSingleton<JwtTokenServices>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICascadingService, CascadingService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>(); // This is a duplicate line here, consider removing one.
builder.Services.AddScoped<DashboardService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.Configure<ItemManagement.Model.SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// ---------------------------------------------------------------------
// 4️⃣  Entity Framework Core (SQL Server)
// ---------------------------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------------------------------------------------------------------
// 5️⃣  JWT Authentication
// ---------------------------------------------------------------------
var jwtKey = builder.Configuration["JwtSettings:Key"];
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"];
var jwtAudience = builder.Configuration["JwtSettings:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = ClaimTypes.Role          // <- maps "role" claim
        };

        // (Optional) save token in HttpContext if you ever need it later
        options.SaveToken = true;
    });


// --- Add CORS Policy ---
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins"; // Define a policy name

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      builder =>
                      {
                          builder.WithOrigins("https://localhost:7285") // <--- IMPORTANT: Your frontend URL
                                 .AllowAnyHeader()
                                 .AllowAnyMethod();
                          // .AllowCredentials(); // Use this if your frontend sends cookies/credentials
                      });
});
// ---------------------------------------------------------------------
// 6️⃣  Role-based Authorization Policies (optional but clean)
// ---------------------------------------------------------------------
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", p => p.RequireRole("SuperAdmin"));
    options.AddPolicy("StoreAdminOnly", p => p.RequireRole("StoreAdmin"));
    options.AddPolicy("UserOnly", p => p.RequireRole("User"));
});

var app = builder.Build();

// ---------------------------------------------------------------------
// 7️⃣  Middleware Pipeline
// ---------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthentication();  // must precede UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();
