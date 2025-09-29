using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MatchFynWebAPI.Data;
using MatchFynWebAPI.Models.Identity;
using MatchFynWebAPI.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for professional logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/matchfyn-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.

// Configure JWT Settings
var jwtSettings = new JwtSettings();
builder.Configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);

// Validate JWT settings
if (!jwtSettings.IsValid())
{
    throw new InvalidOperationException("JWT settings are not properly configured. Please check appsettings.json");
}

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

// Add Entity Framework - Multiple DbContexts
builder.Services.AddDbContext<MatchFynDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityConnection")));

// Add Identity Framework
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Password settings - Professional security standards
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings - Prevent brute force attacks
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = true;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<IdentityDbContext>()
.AddDefaultTokenProviders();

// Add JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // Set to true in production
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = jwtSettings.ValidateIssuer,
        ValidateAudience = jwtSettings.ValidateAudience,
        ValidateLifetime = jwtSettings.ValidateLifetime,
        ValidateIssuerSigningKey = jwtSettings.ValidateIssuerSigningKey,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew = jwtSettings.ClockSkew
    };
});

// Add Authorization
builder.Services.AddAuthorization(options =>
{
    // Define authorization policies
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole(ApplicationRoles.Admin));
    options.AddPolicy("RequireModeratorRole", policy => policy.RequireRole(ApplicationRoles.Admin, ApplicationRoles.Moderator));
    options.AddPolicy("RequirePremiumUser", policy => policy.RequireRole(ApplicationRoles.Admin, ApplicationRoles.PremiumUser));
});

// Add CORS for Flutter app - More secure configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutter", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000") // Add your Flutter app URLs
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Allow credentials for JWT
    });
});

// Add AutoMapper for object mapping
builder.Services.AddAutoMapper(typeof(Program));

// Add custom services
builder.Services.AddScoped<MatchFynWebAPI.Services.ITokenService, MatchFynWebAPI.Services.TokenService>();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "MatchFyn API", 
        Version = "v1",
        Description = "Professional Social Media API for MatchFyn Application"
    });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS - Must be before Authentication and Authorization
app.UseCors("AllowFlutter");

// Use Authentication and Authorization - Order is important!
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Add professional logging
app.UseSerilogRequestLogging();

try
{
    Log.Information("Starting MatchFyn API application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MatchFyn API application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
