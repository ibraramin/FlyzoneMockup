using System.Net;
using Flyzone.Data;
using Flyzone.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlyzoneMockup.Tests;

public class ServiceCatalogTests : IClassFixture<TestAppFactory>
{
    private const string Password = "Test@1234";
    private readonly TestAppFactory _factory;

    public ServiceCatalogTests(TestAppFactory factory) => _factory = factory;

    [Fact]
    public async Task LandingPage_RendersActiveCatalogRows_FromDatabase()
    {
        var client = HttpTestHelpers.CreateClient(_factory);
        var html = await client.GetStringAsync("/");

        Assert.Contains("Golden Visa Application", html);
        Assert.Contains("Driving License Renewal", html);
        Assert.Contains("Business License Setup", html);
        Assert.Contains("From AED 2,800", html);

        Assert.DoesNotContain("Service 1", html);
        Assert.DoesNotContain("quick brown fox", html);
        Assert.DoesNotContain("Instant Scheduling", html);
    }

    [Fact]
    public async Task LandingPage_HidesInactiveCatalogRows()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.TypingServices.Add(new TypingService
            {
                Category = "Test Only",
                ServiceName = "Discontinued Service XYZ",
                BaseFee = 1m,
                IsActive = false
            });
            await db.SaveChangesAsync();
        }

        var client = HttpTestHelpers.CreateClient(_factory);
        var html = await client.GetStringAsync("/");
        Assert.DoesNotContain("Discontinued Service XYZ", html);
    }

    [Fact]
    public async Task GoldenVisaSubmission_UsesCatalogServiceId_NotHardcodedInteger()
    {
        // Re-key the seeded Golden Visa row so the expected id cannot equal the
        // old hard-coded literal (1). A throwaway fixture DB makes this safe.
        const int catalogId = 101;
        using (var setupScope = _factory.Services.CreateScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var seeded = await db.TypingServices.SingleAsync(s => s.Id == 1);
            db.TypingServices.Remove(seeded);
            await db.SaveChangesAsync();
            db.TypingServices.Add(new TypingService
            {
                Id = catalogId,
                Category = seeded.Category,
                ServiceName = seeded.ServiceName,
                Description = seeded.Description,
                BaseFee = seeded.BaseFee,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        var userId = await CreateUserAsync("catalog-applicant@example.com");
        var client = await HttpTestHelpers.LoginAsync(_factory, "catalog-applicant@example.com", Password);
        var token = await HttpTestHelpers.GetTokenAsync(client, "/Services/GoldenVisa");

        var response = await client.PostAsync("/Services/GoldenVisa", HttpTestHelpers.Form(
            ("PassportNumber", "P1234567"),
            ("Profession", "Engineer"),
            ("MonthlySalary", "25000"),
            ("IsInvestorCategory", "false"),
            ("__RequestVerificationToken", token)));

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var application = await verifyDb.ServiceApplications.SingleAsync(a => a.UserId == userId);
        Assert.Equal(catalogId, application.ServiceId);
        Assert.Equal("Golden Visa Application",
            (await verifyDb.TypingServices.SingleAsync(s => s.Id == application.ServiceId)).ServiceName);
    }

    private async Task<string> CreateUserAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Catalog",
            LastName = "Tester",
            CreatedAt = DateTime.UtcNow
        };
        var result = await users.CreateAsync(user, Password);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
        return user.Id;
    }
}
