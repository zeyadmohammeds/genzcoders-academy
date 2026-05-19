using GenZCoders.Data;
using GenZCoders.Hubs;
using GenZCoders.Models;
using GenZCoders.Models.Identity;
using GenZCoders.Repos;
using GenZCoders.Services;
using GenZCoders.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sentry;
using System.Text.Json.Serialization;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ─── Sentry Error Monitoring ────────────────────────────────────────────────
builder.WebHost.UseSentry(o =>
{
    o.Dsn = "https://67ea5ad415be8071ce91508ee72b8518@o4509983381323776.ingest.de.sentry.io/4511398461309008";
    o.Debug = true; // Enable internal Sentry logs (equivalent to EnableLogs)
    o.DiagnosticLevel = SentryLevel.Debug;
    o.TracesSampleRate = builder.Environment.IsDevelopment() ? 1.0 : 0.2;
    o.MaxBreadcrumbs = 100;
    o.SendDefaultPii = true;
    o.MinimumBreadcrumbLevel = Microsoft.Extensions.Logging.LogLevel.Information;
    o.MinimumEventLevel = Microsoft.Extensions.Logging.LogLevel.Warning;
    o.AttachStacktrace = true;
    o.Environment = builder.Environment.EnvironmentName;
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");

builder.Services.AddDbContext<AcademyDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AcademyDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "GenZCoders.Auth";
    if (builder.Environment.IsDevelopment())
    {
        // For local development on HTTP, Lax and SameAsRequest allow authentication cookies to work
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    }
    else
    {
        options.Cookie.SameSite = SameSiteMode.None; // Required for Vercel + RunASP cross-domain
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Required for SameSite=None
    }
    options.LoginPath = "/api/auth/google";
    options.AccessDeniedPath = "/api/auth/access-denied";
    options.SlidingExpiration = true;
});

builder.Services.ConfigureExternalCookie(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    }
    else
    {
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }
});

builder.Services.AddAuthentication()
    .AddGoogle("Google", options =>
    {
        IConfigurationSection googleAuthSection = builder.Configuration.GetSection("Authentication:Google");
        options.ClientId = googleAuthSection["ClientId"] ?? "";
        options.ClientSecret = googleAuthSection["ClientSecret"] ?? "";
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("NextJsClient", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ??
                           ["http://localhost:3000", "https://localhost:3000", "https://genzacademy.vercel.app"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AcademyStaff", policy => policy.RequireRole(AcademyRole.AcademyAdmin, AcademyRole.Engineer, AcademyRole.Cta));
    options.AddPolicy("CourseManagers", policy => policy.RequireRole(AcademyRole.AcademyAdmin, AcademyRole.Engineer));
    options.AddPolicy("StudentsOnly", policy => policy.RequireRole(AcademyRole.Student));
});
builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen(); // Removed in favor of Microsoft.AspNetCore.OpenApi + Scalar
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IZoomService, ZoomService>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<ILiveSessionService, LiveSessionService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReferralService, ReferralService>();
builder.Services.AddScoped<IAuthWorkflowService, AuthWorkflowService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<ICourseRoundService, CourseRoundService>();
builder.Services.AddScoped<ICourseRoomService, CourseRoomService>();
builder.Services.AddScoped<ILearningService, LearningService>();
builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.Services.AddScoped<ICourseRecommendationService, CourseRecommendationService>();
builder.Services.AddSignalR();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AcademyDbContext>("Database", tags: new[] { "db" });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("ElSewedy Academy API")
               .WithTheme(ScalarTheme.Mars)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// ─── Health Checks ──────────────────────────────────────────────────────────
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            details = report.Entries.Select(e => new
            {
                key = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                error = e.Value.Exception?.Message
            })
        });
        await context.Response.WriteAsync(result);
    }
});

// ─── Automated Database Migrations ──────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AcademyDbContext>();
        // Diagnostic log for connection string presence
        var conn = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(conn))
        {
            SentrySdk.CaptureMessage("⚠️ DB Connection string is NULL or Empty!", SentryLevel.Warning);
        }

        // ─── Self-Healing & Migrations ──────────────────────────────────────────
        SentrySdk.CaptureMessage($"🛠️ DB Startup - Env: {app.Environment.EnvironmentName}", SentryLevel.Info);
        
        try 
        {
            // 1. Try Migrations
            await db.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureMessage($"⚠️ Migration warning (might be pre-existing tables): {ex.Message}");
        }

        try 
        {
            // 2. Force Self-Healing (Add missing columns manually)
            await EnsureIdentityColumnsExist(db);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }

        try 
        {
            // 3. Seed initial data
            await AcademySeeder.SeedAsync(services);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
        SentrySdk.CaptureException(ex);
    }
}

app.UseHttpsRedirection();
app.UseRouting();
if (app.Environment.IsDevelopment())
{
    app.UseCookiePolicy(new CookiePolicyOptions
    {
        MinimumSameSitePolicy = SameSiteMode.Lax,
        Secure = CookieSecurePolicy.SameAsRequest
    });
}
else
{
    app.UseCookiePolicy(new CookiePolicyOptions
    {
        MinimumSameSitePolicy = SameSiteMode.None,
        Secure = CookieSecurePolicy.Always
    });
}
app.UseSentryTracing(); // Must be after UseRouting and before UseAuthorization
app.UseCors("NextJsClient");
app.UseAuthentication();
app.UseSentryUserContext();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<AdminHub>("/hubs/admin");

// ─── Sentry verification on startup ────────────────────────────────────────
SentrySdk.CaptureMessage("🚀 GenZCoders Backend started — Sentry is live!", SentryLevel.Info);

// Test logs to verify Sentry integration
// Note: SentrySdk.CaptureMessage is the standard way in .NET to send test logs
SentrySdk.CaptureMessage("A simple log message", SentryLevel.Info);
SentrySdk.CaptureMessage(string.Format("A {0} log message", "formatted"), SentryLevel.Error);

app.Lifetime.ApplicationStopping.Register(() =>
{
    SentrySdk.Flush(TimeSpan.FromSeconds(5));
});

app.Run();

async Task EnsureIdentityColumnsExist(AcademyDbContext db)
{
    var columnsToAdd = new Dictionary<string, string>
    {
        { "FirstName", "NVARCHAR(MAX) DEFAULT ''" },
        { "LastName", "NVARCHAR(MAX) DEFAULT ''" },
        { "Bio", "NVARCHAR(MAX) NULL" },
        { "RoleKey", "NVARCHAR(MAX) DEFAULT 'student'" },
        { "AvatarUrl", "NVARCHAR(MAX) NULL" },
        { "PreferredLanguage", "NVARCHAR(MAX) DEFAULT 'en'" },
        { "City", "NVARCHAR(MAX) NULL" },
        { "LastLoginAt", "DATETIMEOFFSET NULL" },
        { "VerifiedAt", "DATETIMEOFFSET NULL" },
        { "IsActive", "BIT DEFAULT 1" },
        { "TotalXp", "INT DEFAULT 0" },
        { "Level", "INT DEFAULT 1" },
        { "ProfileCompleted", "BIT DEFAULT 0" },
        { "Age", "INT NULL" },
        { "GradeLevel", "NVARCHAR(MAX) NULL" },
        { "NationalId", "NVARCHAR(MAX) NULL" },
        { "SchoolName", "NVARCHAR(MAX) NULL" },
        { "ExperienceLevel", "NVARCHAR(MAX) NULL" },
        { "PreferredTrack", "NVARCHAR(MAX) NULL" },
        { "Goals", "NVARCHAR(MAX) NULL" },
        { "InterestsJson", "NVARCHAR(MAX) NULL" },
        { "CreatedAt", "DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()" },
        { "UpdatedAt", "DATETIMEOFFSET DEFAULT SYSDATETIMEOFFSET()" }
    };

    foreach (var col in columnsToAdd)
    {
        try
        {
            // Check if column exists in AspNetUsers
            string checkSql = $"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[AspNetUsers]') AND name = '{col.Key}') " +
                              $"ALTER TABLE [AspNetUsers] ADD [{col.Key}] {col.Value};";
            await db.Database.ExecuteSqlRawAsync(checkSql);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    // Also check AspNetRoles
    try
    {
        string roleCheckSql = "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[AspNetRoles]') AND name = 'Description') " +
                              "ALTER TABLE [AspNetRoles] ADD [Description] NVARCHAR(MAX) DEFAULT '';";
        await db.Database.ExecuteSqlRawAsync(roleCheckSql);
    }
    catch (Exception ex)
    {
        SentrySdk.CaptureException(ex);
    }

    // Self-heal Courses table missing columns
    var courseColumnsToAdd = new Dictionary<string, string>
    {
        { "ImageUrl", "NVARCHAR(MAX) NULL" },
        { "IsDeleted", "BIT DEFAULT 0" }
    };

    foreach (var col in courseColumnsToAdd)
    {
        try
        {
            string checkSql = $"IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[Courses]') AND name = '{col.Key}') " +
                              $"ALTER TABLE [Courses] ADD [{col.Key}] {col.Value};";
            await db.Database.ExecuteSqlRawAsync(checkSql);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }
}
