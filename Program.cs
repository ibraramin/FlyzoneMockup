using Flyzone.Data;
using Flyzone.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// --- Kestrel Request Body Size Limit (DoS Prevention) ---
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// --- Rate Limiting ---
builder.Services.AddMemoryCache();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429; // TooManyRequests
    
    // Auth Rate Limit: 10 requests per minute (global fixed window)
    options.AddFixedWindowLimiter("AuthRateLimit", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    // Form Submit Rate Limit: 5 requests per minute (global fixed window)
    options.AddFixedWindowLimiter("FormSubmitLimit", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});

// Register SQLite Database. Resolve the connection string lazily so test hosts
// can override it before the first DbContext is created.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Identity (Users & Roles) with strict security
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Password Policy: strict requirements
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 4;

    // Lockout Settings: brute-force protection
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User Settings
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    // Sign In Settings
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Persist the Data Protection key ring so antiforgery tokens stay valid across
// container restarts (the default key store is ephemeral inside a container).
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"]
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Flyzone", "keys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("Flyzone");

// Harden the Identity application cookie. Secure cookies are only forced outside
// Development so local HTTP and the integration-test host keep working.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".Flyzone.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Home/Error";
});

var app = builder.Build();

// --- Automatic Database Migration ---
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying database migrations...");
        dbContext.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying migrations.");
        // A broken schema must never be served in Production. Development keeps
        // the tolerate-and-continue behaviour for local convenience.
        if (!app.Environment.IsDevelopment())
        {
            throw;
        }
    }

    try
    {
        // Opt-in demo accounts/applications: enabled for the local and container
        // walkthrough, disabled by default so tests and real deployments stay clean.
        await DemoDataSeeder.SeedAsync(scope.ServiceProvider, builder.Configuration, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding demo data.");
        if (!app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}
// -------------------------------------

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// --- Security Headers Middleware ---
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    // Content Security Policy (CSP). Page scripts are served from static files;
    // inline/eval scripts are not allowed. Leaflet is vendored locally, so no
    // script or style CDN is needed for the map. The only map tile origin is
    // the keyless OpenStreetMap endpoint; ui-avatars.com serves the existing
    // dashboard avatar images. No other tile provider is permitted.
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; " +
        "script-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
        "img-src 'self' data: https://tile.openstreetmap.org https://ui-avatars.com; " +
        "font-src 'self' https://cdnjs.cloudflare.com; " +
        "connect-src 'self' https://tile.openstreetmap.org; " +
        "frame-ancestors 'none';";
    
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// Enable rate limiting
app.UseRateLimiter();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Exposed so integration tests can use WebApplicationFactory<Program>.
public partial class Program { }
