using Flyzone.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Flyzone.Data;

/// <summary>
/// Opt-in walkthrough data: a role-gated admin account, one applicant account and
/// a few applications with status history. Enabled only when DemoData:Enabled=true.
/// The credentials are public on purpose — this is demo-only data, never a real
/// deployment path.
/// </summary>
public static class DemoDataSeeder
{
    public const string AdminRole = "Admin";
    public const string AdminEmail = "admin@flyzone.example";
    public const string AdminPassword = "Admin@1234";
    public const string DemoEmail = "demo@flyzone.example";
    public const string DemoPassword = "Demo@1234";
    private const string SeedNote = "Seeded walkthrough application.";

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger)
    {
        if (!configuration.GetValue<bool>("DemoData:Enabled"))
        {
            logger.LogInformation("Demo data seeding is disabled.");
            return;
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();

        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(AdminRole));
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not create the demo Admin role: " +
                    string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }
        }

        await EnsureUserAsync(userManager, AdminEmail, AdminPassword, "Ada", "Admin");
        await EnsureUserAsync(userManager, DemoEmail, DemoPassword, "Demo", "Applicant");

        var admin = await userManager.FindByEmailAsync(AdminEmail);
        if (admin != null && !await userManager.IsInRoleAsync(admin, AdminRole))
        {
            var addResult = await userManager.AddToRoleAsync(admin, AdminRole);
            if (!addResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not assign the demo Admin role: " +
                    string.Join("; ", addResult.Errors.Select(e => e.Description)));
            }
        }

        var applicant = await userManager.FindByEmailAsync(DemoEmail);
        if (applicant == null || await db.ServiceApplications
                .AnyAsync(a => a.UserId == applicant.Id && a.InternalNotes == SeedNote))
        {
            logger.LogInformation("Demo applications already present; skipping demo seeding.");
            return;
        }

        var catalog = await db.TypingServices
            .AsNoTracking()
            .ToDictionaryAsync(s => s.ServiceName, s => s.Id);

        var samples = new[]
        {
            ("Golden Visa Application", ApplicationStatus.UnderReview,
                "Documents received; reviewing supporting evidence.", DateTime.UtcNow.AddDays(-12)),
            ("Driving License Renewal", ApplicationStatus.WaitingPayment,
                "Review complete; payment requested.", DateTime.UtcNow.AddDays(-6)),
            ("Emirates ID Renewal", ApplicationStatus.Completed,
                "Completed and handed over for dispatch.", DateTime.UtcNow.AddDays(-30))
        };

        var added = 0;
        foreach (var (serviceName, status, note, submittedAt) in samples)
        {
            if (!catalog.TryGetValue(serviceName, out var serviceId))
            {
                continue;
            }

            var application = new ServiceApplication
            {
                UserId = applicant.Id,
                ServiceId = serviceId,
                Status = status,
                CreatedAt = submittedAt,
                UpdatedAt = submittedAt.AddDays(2),
                InternalNotes = SeedNote,
                StatusHistory = new List<ApplicationHistory>
                {
                    new()
                    {
                        StatusState = ApplicationStatus.Submitted,
                        Comments = "Application submitted",
                        Timestamp = submittedAt
                    },
                    new()
                    {
                        StatusState = status,
                        Comments = note,
                        Timestamp = submittedAt.AddDays(2)
                    }
                }
            };

            db.ServiceApplications.Add(application);
            added++;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Demo role, accounts and {Count} applications seeded.", added);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string firstName,
        string lastName)
    {
        if (await userManager.FindByEmailAsync(email) != null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create demo user {email}: " +
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
