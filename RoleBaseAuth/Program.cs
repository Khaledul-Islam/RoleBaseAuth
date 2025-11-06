using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RoleBaseAuth.Application.Common.Behaviors;
using RoleBaseAuth.Application.Common.Mappings;
using RoleBaseAuth.Application.Features.Auth.Commands;
using RoleBaseAuth.Application.Validators.Auth;
using RoleBaseAuth.Authorization;
using RoleBaseAuth.Domain.Enum;
using RoleBaseAuth.Domain.Interfaces;
using RoleBaseAuth.Infrastructure.Caching;
using RoleBaseAuth.Infrastructure.Identity;
using RoleBaseAuth.Infrastructure.Persistence.Contexts;
using RoleBaseAuth.Infrastructure.Persistence.Repositories;
using RoleBaseAuth.Infrastructure.Persistence.UnitOfWorks;
using RoleBaseAuth.Infrastructure.Persistence;
using RoleBaseAuth.Middleware;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Asp.Versioning;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Configure Serilog
// ============================================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"]) // Optional: Seq for log aggregation
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================================
// Add Services to Container
// ============================================================

// Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("CleanArchitectureApi.Infrastructure")));

// Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "CleanArchApi_";
});

// Repositories & UoW
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddHttpContextAccessor();

// MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(LoginCommand).Assembly);
});

// MediatR Pipeline Behaviors
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(LoginRequestValidator).Assembly);

// AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MappingProfile>();
});


// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // Permission-based policies
    options.AddPolicy("RequireCustomersViewPermission", policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.CustomersView)));

    options.AddPolicy("RequireCustomersCreatePermission", policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.CustomersCreate)));

    options.AddPolicy("RequireCustomersEditPermission", policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.CustomersEdit)));

    options.AddPolicy("RequireCustomersDeletePermission", policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.CustomersDelete)));

    options.AddPolicy("RequireCustomersExportPermission", policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.CustomersExport)));

    // Role-based policies
    options.AddPolicy("RequireSuperAdminRole", policy =>
        policy.RequireRole(Roles.SuperAdmin));

    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole(Roles.SuperAdmin, Roles.Admin));
});

builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

//builder.Services.AddVersionedApiExplorer(options =>
//{
//    options.GroupNameFormat = "'v'VVV";
//    options.SubstituteApiVersionInUrl = true;
//});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddRedis(builder.Configuration.GetConnectionString("Redis"));

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Clean Architecture API",
        Version = "v1",
        Description = "ASP.NET Core API with Clean Architecture, CQRS, and JWT Authentication",
        Contact = new OpenApiContact
        {
            Name = "Your Name",
            Email = "your.email@example.com"
        }
    });

    // JWT Authentication in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Background Jobs (Quartz.NET) - Optional
/*
builder.Services.AddQuartz(q =>
{
    // Configure jobs here
});
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
*/

var app = builder.Build();

// ============================================================
// Database Migration & Seeding
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var authService = services.GetRequiredService<IAuthService>();
        var logger = services.GetRequiredService<ILogger<DbSeeder>>();

        var seeder = new DbSeeder(context, authService, logger);
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database");
    }
}

// ============================================================
// Configure HTTP Pipeline
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Clean Architecture API v1");
    });
}

// Custom Middleware
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

// Standard Middleware
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Health Checks Endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

app.MapControllers();

// Serilog Request Logging
app.UseSerilogRequestLogging();

Log.Information("Starting Clean Architecture API...");

app.Run();