using System.Net;
using Flyzone.Data;
using Flyzone.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlyzoneMockup.Tests;

public class AdminFlowTests : IClassFixture<TestAppFactory>
{
    private const string Password = "Test@1234";
    private readonly TestAppFactory _factory;

    public AdminFlowTests(TestAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Admin_CanListAndOpenApplications()
    {
        var (applicantId, applicationId) = await CreateApplicationAsync("admin-list@example.com");
        await CreateAdminAsync("admin-list-officer@example.com");

        var client = await HttpTestHelpers.LoginAsync(_factory, "admin-list-officer@example.com", Password);
        var list = await client.GetStringAsync("/Admin");
        Assert.Contains("Admin Review Queue", list);
        Assert.Contains("admin-list@example.com", list);
        Assert.Contains(applicationId, list);

        var details = await client.GetStringAsync($"/Admin/Details/{applicationId}");
        Assert.Contains("Review Application", details);
        Assert.Contains("Application Timeline", details);
        Assert.Contains("admin-list@example.com", details);
    }

    [Fact]
    public async Task NonAdmin_IsForbiddenFromAdminQueue()
    {
        await CreateUserAsync("non-admin@example.com");
        var client = await HttpTestHelpers.LoginAsync(_factory, "non-admin@example.com", Password);
        var response = await client.GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/Home/Error", response.Headers.Location?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task Admin_UpdateStatus_AppendsHistoryInOneTransaction()
    {
        var (_, applicationId) = await CreateApplicationAsync("admin-update@example.com");
        await CreateAdminAsync("admin-update-officer@example.com");
        var client = await HttpTestHelpers.LoginAsync(_factory, "admin-update-officer@example.com", Password);
        var token = await HttpTestHelpers.GetTokenAsync(client, $"/Admin/Details/{applicationId}");

        var response = await client.PostAsync("/Admin/Update", HttpTestHelpers.Form(
            ("id", applicationId),
            ("status", "Completed"),
            ("internalNotes", "  Verified by officer.  "),
            ("comment", "All checks passed."),
            ("__RequestVerificationToken", token)));

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains($"/Admin/Details/{applicationId}", response.Headers.Location?.ToString() ?? string.Empty);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updated = await db.ServiceApplications
            .Include(a => a.StatusHistory)
            .SingleAsync(a => a.Id == applicationId);
        Assert.Equal(ApplicationStatus.Completed, updated.Status);
        Assert.Equal("Verified by officer.", updated.InternalNotes);
        Assert.Contains(updated.StatusHistory, h =>
            h.StatusState == ApplicationStatus.Completed && h.Comments == "All checks passed.");
    }

    [Fact]
    public async Task Admin_Update_WithoutAntiforgeryToken_IsRejected()
    {
        var (_, applicationId) = await CreateApplicationAsync("admin-token@example.com");
        await CreateAdminAsync("admin-token-officer@example.com");
        var client = await HttpTestHelpers.LoginAsync(_factory, "admin-token-officer@example.com", Password);

        var response = await client.PostAsync("/Admin/Update", HttpTestHelpers.Form(
            ("id", applicationId),
            ("status", "Completed")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_Update_WithUnknownStatus_IsRejected()
    {
        var (_, applicationId) = await CreateApplicationAsync("admin-badstatus@example.com");
        await CreateAdminAsync("admin-badstatus-officer@example.com");
        var client = await HttpTestHelpers.LoginAsync(_factory, "admin-badstatus-officer@example.com", Password);
        var token = await HttpTestHelpers.GetTokenAsync(client, $"/Admin/Details/{applicationId}");

        var response = await client.PostAsync("/Admin/Update", HttpTestHelpers.Form(
            ("id", applicationId),
            ("status", "Bogus"),
            ("__RequestVerificationToken", token)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var application = await db.ServiceApplications
            .Include(a => a.StatusHistory)
            .SingleAsync(a => a.Id == applicationId);
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Single(application.StatusHistory);
    }

    private async Task<(string ApplicantId, string ApplicationId)> CreateApplicationAsync(string email)
    {
        var applicantId = await CreateUserAsync(email);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var serviceId = await db.TypingServices
            .Where(s => s.ServiceName == "Golden Visa Application")
            .Select(s => s.Id)
            .SingleAsync();
        var application = new ServiceApplication
        {
            UserId = applicantId,
            ServiceId = serviceId,
            Status = ApplicationStatus.Submitted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            StatusHistory = new List<ApplicationHistory>
            {
                new()
                {
                    StatusState = ApplicationStatus.Submitted,
                    Comments = "Application submitted",
                    Timestamp = DateTime.UtcNow
                }
            }
        };
        db.ServiceApplications.Add(application);
        await db.SaveChangesAsync();
        return (applicantId, application.Id);
    }

    private async Task CreateAdminAsync(string email)
    {
        var userId = await CreateUserAsync(email);
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync(DemoDataSeeder.AdminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(DemoDataSeeder.AdminRole));
        }
        var user = await userManager.FindByIdAsync(userId);
        Assert.NotNull(user);
        var result = await userManager.AddToRoleAsync(user, DemoDataSeeder.AdminRole);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    private async Task<string> CreateUserAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Admin",
            LastName = "Flow",
            CreatedAt = DateTime.UtcNow
        };
        var result = await users.CreateAsync(user, Password);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
        return user.Id;
    }
}
