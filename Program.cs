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

var builder = WebApplication.CreateBuilder(args);

// ─── Sentry Error Monitoring ────────────────────────────────────────────────
builder.WebHost.UseSentry(o =>
{
    o.Dsn = "https://67ea5ad415be8071ce91508ee72b8518@o4509983381323776.ingest.de.sentry.io/4511398461309008";
    o.Debug = builder.Environment.IsDevelopment();
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
    options.LoginPath = "/api/auth/google";
    options.AccessDeniedPath = "/access-denied.html";
    options.SlidingExpiration = true;
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
                           ["http://localhost:3000", "https://localhost:3000"])
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
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AcademyDbContext>();
    await db.Database.MigrateAsync();
    await AcademySeeder.SeedAsync(scope.ServiceProvider);
}

app.UseHttpsRedirection();
app.UseRouting();
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

app.Lifetime.ApplicationStopping.Register(() =>
{
    SentrySdk.Flush(TimeSpan.FromSeconds(5));
});

app.Run();
