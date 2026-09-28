using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SmartGym.Api.Configuration;
using SmartGym.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Configuration Options
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<AiServiceSettings>(builder.Configuration.GetSection(AiServiceSettings.SectionName));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));

// 2. Add MVC Controllers, Health Checks & Database Context
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

builder.Services.AddDbContext<SmartGym.Api.Data.SmartGymDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<SmartGym.Api.Data.ITransactionService, SmartGym.Api.Data.TransactionService>();
builder.Services.AddScoped<SmartGym.Api.Data.DatabaseSeeder>();

// 3. Configure CORS Policy
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

// 4. Configure Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartGym API",
        Version = "v1",
        Description = "Authoritative REST API for SmartGym — Integrated Gym, Supplement, Membership & Facility Management System with Agentic AI."
    });

    // JWT Bearer Authentication Definition
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token directly (e.g. eyJhbGciOi...)"
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

// 5. Global Middleware Pipeline
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

app.UseRouting();

// Map Health Checks & Controllers
app.MapHealthChecks("/health");
app.MapControllers();

// Root redirect to Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger"));

// 6. Database Seeding in Development
if (app.Environment.IsDevelopment() && !app.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<SmartGym.Api.Data.DatabaseSeeder>();
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
