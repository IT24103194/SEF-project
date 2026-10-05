using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartGym.Api.Authentication;
using SmartGym.Api.Authorization;
using SmartGym.Api.Configuration;
using SmartGym.Api.Data;
using SmartGym.Api.Middleware;
using SmartGym.Api.Services;
using SmartGym.Api.Validators;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Configuration Options with DataAnnotations Validation
builder.Services.AddOptions<JwtSettings>()
    .BindConfiguration(JwtSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<AiServiceSettings>()
    .BindConfiguration(AiServiceSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<EmailSettings>()
    .BindConfiguration(EmailSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// 2. Add MVC Controllers with Custom RFC 7807 Validation Response & Health Checks
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = ValidationInfrastructure.CreateValidationProblemResponse;
    });

builder.Services.AddHealthChecks();

// 3. Configure Database Context & Core Services
builder.Services.AddDbContext<SmartGymDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IContentModerationService, ContentModerationService>();
builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
builder.Services.AddScoped<IFacilityService, FacilityService>();
builder.Services.AddScoped<IClassManagementService, ClassManagementService>();
builder.Services.AddScoped<IMembershipService, MembershipService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<SmartGym.Api.Services.Email.IEmailService, SmartGym.Api.Services.Email.LocalDevelopmentEmailService>();
builder.Services.AddHttpClient<SmartGym.Api.Services.Email.ExternalEmailService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IAiServiceClient, AiServiceClient>();

// 4. Configure JWT Authentication & Authorization
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
var key = Encoding.UTF8.GetBytes(jwtSettings.Key);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // Strict expiry check
    };

    // Custom 401 Unauthorized handling with RFC 7807 ProblemDetails
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";

            var correlationId = context.HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString()
                ?? context.HttpContext.TraceIdentifier;

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = string.IsNullOrWhiteSpace(context.ErrorDescription)
                    ? "Authentication token is missing, invalid, or expired."
                    : context.ErrorDescription,
                Instance = context.Request.Path
            };
            problemDetails.Extensions["correlationId"] = correlationId;
            problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

            var json = System.Text.Json.JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(json);
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";

            var correlationId = context.HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString()
                ?? context.HttpContext.TraceIdentifier;

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = "You do not have permission to access this resource.",
                Instance = context.Request.Path
            };
            problemDetails.Extensions["correlationId"] = correlationId;
            problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

            var json = System.Text.Json.JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(json);
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPolicies.RequireAdmin, policy =>
        policy.RequireRole(AppRoles.Admin, "ADMIN"));

    options.AddPolicy(AppPolicies.RequireTrainer, policy =>
        policy.RequireRole(AppRoles.Trainer, AppRoles.Admin, "TRAINER", "ADMIN"));

    options.AddPolicy(AppPolicies.RequireMember, policy =>
        policy.RequireRole(AppRoles.Member, AppRoles.Admin, "MEMBER", "ADMIN"));

    options.AddPolicy(AppPolicies.RequireStaff, policy =>
        policy.RequireRole(AppRoles.Trainer, AppRoles.Admin, "TRAINER", "ADMIN"));
});

// 5. Configure CORS Policy
var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>() ?? new[]
{
    "http://localhost:3000",
    "http://localhost:5173",
    "http://127.0.0.1:3000",
    "http://127.0.0.1:5173"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("SmartGymCors", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 6. Configure Swagger / OpenAPI with Interactive JWT Bearer Support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartGym API",
        Version = "v1",
        Description = "Authoritative REST API for SmartGym — Integrated Gym, Supplement, Membership & Facility Management System with Agentic AI."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT Bearer token directly (e.g. eyJhbGciOi...)"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

var app = builder.Build();

// 7. Global Middleware Pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

// Enable Swagger in all environments for academic evaluation & viva demonstration
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartGym API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("SmartGymCors");
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map Health Checks & Controllers
app.MapHealthChecks("/health");
app.MapControllers();

// Root redirect to Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger"));

// 8. Database Seeding in Development and Testing
// Note: We enable seeding in the 'Testing' environment so that integration tests have access to the required seed data.
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    try
    {
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Database seeding could not complete automatically on startup.");
    }
}

app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }
