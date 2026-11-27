using System.Net;
using Flyzone.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlyzoneMockup.Tests;

public class SecurityHeadersTests : IClassFixture<TestAppFactory>
{
    private const string Password = "Test@1234";
    private readonly TestAppFactory _factory;

    public SecurityHeadersTests(TestAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Csp_ForbidsInlineAndEvalScripts()
    {
        var client = HttpTestHelpers.CreateClient(_factory);
        var response = await client.GetAsync("/");
        var csp = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));

        Assert.Contains("script-src 'self'", csp);
        Assert.DoesNotContain("'unsafe-eval'", csp);
        var scriptSources = csp.Split("script-src", StringSplitOptions.None)[1].Split(';')[0];
        Assert.DoesNotContain("'unsafe-inline'", scriptSources);
    }

    [Fact]
    public async Task SecurityHeaders_ArePresentOnResponses()
    {
        var client = HttpTestHelpers.CreateClient(_factory);
        var response = await client.GetAsync("/Account/Login");

        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("strict-origin-when-cross-origin", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
    }

    [Fact]
    public async Task AuthCookie_IsHttpOnlyAndSameSiteLax()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var created = await users.CreateAsync(new ApplicationUser
            {
                UserName = "cookieuser@example.com",
                Email = "cookieuser@example.com",
                FirstName = "Cookie",
                LastName = "User",
                CreatedAt = DateTime.UtcNow
            }, Password);
            Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        var (client, token) = await HttpTestHelpers.GetFormAsync(_factory, "/Account/Login");
        var response = await client.PostAsync("/Account/Login", HttpTestHelpers.Form(
            ("Email", "cookieuser@example.com"),
            ("Password", Password),
            ("RememberMe", "false"),
            ("__RequestVerificationToken", token)));

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var authCookie = response.Headers.GetValues("Set-Cookie")
            .Single(c => c.Contains(".Flyzone.Auth", StringComparison.Ordinal));
        Assert.Contains("httponly", authCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", authCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MigrationFailure_InProduction_PreventsStartup()
    {
        using var factory = new BrokenSchemaFactory();
        Assert.ThrowsAny<Exception>(() => factory.CreateClient());
    }

    private sealed class BrokenSchemaFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // /dev/null is a file, so SQLite cannot create a database beneath it.
                    ["ConnectionStrings:DefaultConnection"] = "Data Source=/dev/null/flyzone.db",
                    ["DataProtection:KeysPath"] = Path.Combine(
                        Path.GetTempPath(), "flyzone-dp-" + Guid.NewGuid().ToString("N"))
                }));
        }
    }
}
