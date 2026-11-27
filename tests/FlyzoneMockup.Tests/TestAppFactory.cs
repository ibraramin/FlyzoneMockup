using Flyzone.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FlyzoneMockup.Tests;

/// <summary>
/// Boots the real application against a throwaway SQLite file so tests never touch
/// the developer's flyzone.db.
/// </summary>
public sealed class TestAppFactory : WebApplicationFactory<Program>, IDisposable
{
    public string DatabasePath { get; }
    public string UploadsPath { get; }
    private readonly string _root;

    public TestAppFactory()
    {
        _root = Path.Combine(Path.GetTempPath(), "flyzone-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        DatabasePath = Path.Combine(_root, "test.db");
        UploadsPath = Path.Combine(_root, "uploads");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Uploads stay inside the throwaway root so the real wwwroot/App_Data
        // folders are never touched by tests; Dispose deletes the whole tree.
        builder.UseSetting("Uploads:Path", UploadsPath);
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={DatabasePath}",
                ["DataProtection:KeysPath"] = Path.Combine(_root, "keys"),
                ["DemoData:Enabled"] = "false"
            }));
    }

    public ApplicationDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_root) && Environment.GetEnvironmentVariable("FLYZONE_KEEP_TEST_DB") != "1")
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
