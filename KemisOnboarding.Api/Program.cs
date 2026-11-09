
using System.Text;
using KemisOnboarding.Api.Data;
using KemisOnboarding.Api.Models;
using KemisOnboarding.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 1. Controllers and API documentation
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 2. CORS configuration for Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 3. Database configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// 4. Password hashing for AppUser
builder.Services.AddScoped<
    IPasswordHasher<AppUser>,
    PasswordHasher<AppUser>>();

// 5. Application services
builder.Services.AddScoped<InstitutionOnboardingService>();
builder.Services.AddScoped<InstitutionWorkflowService>();
builder.Services.AddScoped<TokenService>();

// 6. Email service
builder.Services.AddScoped<
    IInstitutionEmailSender,
    LoggingEmailSender>();

// 7. JWT configuration
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Preserve original JWT claim names such as "sub" and "role".
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)),

        NameClaimType = "name",
        RoleClaimType = "role"
    };
});

// 8. Authorization
builder.Services.AddAuthorization();

// 9. Build application
var app = builder.Build();

// 10. OpenAPI in development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 11. Middleware pipeline
// app.UseHttpsRedirection(); // Temporarily disabled for local HTTP testing

app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

// 12. Map controllers
app.MapControllers();

// 13. Start application
app.Run();
