using System.Net;
using Flyzone.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace FlyzoneMockup.Tests;

public class AccountFlowTests : IClassFixture<TestAppFactory>
{
    private const string Password = "Test@1234";
    private readonly TestAppFactory _factory;

    public AccountFlowTests(TestAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Registration_SignsInAndShowsDashboard()
    {
        var (client, token) = await HttpTestHelpers.GetFormAsync(_factory, "/Account/Register");

        var response = await client.PostAsync("/Account/Register", HttpTestHelpers.Form(
            ("FirstName", "Ada"),
            ("LastName", "Lovelace"),
            ("Email", "ada@example.com"),
            ("PhoneNumber", "+971 50 123 4567"),
            ("Password", Password),
            ("ConfirmPassword", Password),
            ("__RequestVerificationToken", token)));

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/Account/Dashboard", response.Headers.Location?.ToString() ?? string.Empty);

        var dashboard = await client.GetStringAsync("/Account/Dashboard");
        Assert.Contains("Ada", dashboard);
    }

    [Fact]
    public async Task FiveFailedLogins_LockTheAccount()
    {
        await CreateUserAsync("lockme@example.com");

        var (client, token) = await HttpTestHelpers.GetFormAsync(_factory, "/Account/Login");

        for (var i = 0; i < 5; i++)
        {
            var failed = await client.PostAsync("/Account/Login", HttpTestHelpers.Form(
                ("Email", "lockme@example.com"),
                ("Password", "Wrong@1234"),
                ("RememberMe", "false"),
                ("__RequestVerificationToken", token)));
            Assert.Equal(HttpStatusCode.OK, failed.StatusCode);
        }

        var lockedOut = await client.PostAsync("/Account/Login", HttpTestHelpers.Form(
            ("Email", "lockme@example.com"),
            ("Password", Password),
            ("RememberMe", "false"),
            ("__RequestVerificationToken", token)));

        Assert.Equal(HttpStatusCode.OK, lockedOut.StatusCode);
        Assert.Contains("locked out", await lockedOut.Content.ReadAsStringAsync(),
            StringComparison.OrdinalIgnoreCase);
    }

    private async Task CreateUserAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var result = await users.CreateAsync(new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Lock",
            LastName = "Test",
            CreatedAt = DateTime.UtcNow
        }, Password);

        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
