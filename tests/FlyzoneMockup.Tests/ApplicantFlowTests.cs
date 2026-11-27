using System.Net;
using Flyzone.Data;
using Flyzone.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlyzoneMockup.Tests;

/// <summary>
/// Applicant-facing document, details and tracking flows. Each test gets its own
/// host so the global FormSubmitLimit rate limiter cannot leak between tests.
/// </summary>
public class ApplicantFlowTests
{
    private const string Password = "Test@1234";
    private const string ServiceName = "Golden Visa Application";

    [Fact]
    public async Task Details_ReturnsNotFoundForAnotherUsersApplication()
    {
        using var factory = new TestAppFactory();
        var ownerId = await CreateUserAsync(factory, "details-owner@example.com");
        await CreateUserAsync(factory, "details-intruder@example.com");
        var applicationId = await CreateApplicationAsync(factory, ownerId);

        var intruder = await HttpTestHelpers.LoginAsync(factory, "details-intruder@example.com", Password);
        var denied = await intruder.GetAsync($"/Services/Details/{applicationId}");

        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        Assert.DoesNotContain(ServiceName, await denied.Content.ReadAsStringAsync());

        var owner = await HttpTestHelpers.LoginAsync(factory, "details-owner@example.com", Password);
        var allowed = await owner.GetAsync($"/Services/Details/{applicationId}");
        var html = await allowed.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Contains(ServiceName, html);
        Assert.Contains("Application Timeline", html);
        Assert.Contains("Application Submitted", html);
    }

    [Fact]
    public async Task Track_DoesNotDiscloseAnotherUsersReference()
    {
        using var factory = new TestAppFactory();
        var ownerId = await CreateUserAsync(factory, "track-owner@example.com");
        await CreateUserAsync(factory, "track-intruder@example.com");
        var applicationId = await CreateApplicationAsync(factory, ownerId);

        var intruder = await HttpTestHelpers.LoginAsync(factory, "track-intruder@example.com", Password);
        var denied = await intruder.GetAsync($"/Services/Track?reference={applicationId}");
        Assert.Equal(HttpStatusCode.OK, denied.StatusCode);
        var deniedHtml = await denied.Content.ReadAsStringAsync();
        Assert.DoesNotContain(ServiceName, deniedHtml);
        Assert.Contains("No application found", deniedHtml);

        var blank = await intruder.GetAsync("/Services/Track");
        Assert.Equal(HttpStatusCode.OK, blank.StatusCode);
        Assert.Contains("Enter your application reference number", await blank.Content.ReadAsStringAsync());

        var owner = await HttpTestHelpers.LoginAsync(factory, "track-owner@example.com", Password);
        var found = await owner.GetAsync($"/Services/Track?reference={applicationId.ToUpperInvariant()}");
        var foundHtml = await found.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Contains(ServiceName, foundHtml);
        Assert.Contains("Application Timeline", foundHtml);
    }

    [Fact]
    public async Task Download_ReturnsNotFoundForAnotherUsersDocument()
    {
        using var factory = new TestAppFactory();
        var ownerId = await CreateUserAsync(factory, "download-owner@example.com");
        await CreateUserAsync(factory, "download-intruder@example.com");
        var documentId = await CreateDocumentAsync(factory, ownerId, "Passport");

        var intruder = await HttpTestHelpers.LoginAsync(factory, "download-intruder@example.com", Password);
        var denied = await intruder.GetAsync($"/Services/DownloadDocument/{documentId}");

        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
    }

    [Fact]
    public async Task Download_ReturnsBytesForOwner()
    {
        using var factory = new TestAppFactory();
        var ownerId = await CreateUserAsync(factory, "download-bytes@example.com");
        var documentId = await CreateDocumentAsync(factory, ownerId, "Passport");

        var owner = await HttpTestHelpers.LoginAsync(factory, "download-bytes@example.com", Password);
        var response = await owner.GetAsync($"/Services/DownloadDocument/{documentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("%PDF-1.4", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task MyDocuments_ListsOnlyOwnersDocuments()
    {
        using var factory = new TestAppFactory();
        var ownerId = await CreateUserAsync(factory, "docs-owner@example.com");
        var otherId = await CreateUserAsync(factory, "docs-other@example.com");
        await CreateDocumentAsync(factory, ownerId, "Passport");
        await CreateDocumentAsync(factory, otherId, "EmiratesId");

        var owner = await HttpTestHelpers.LoginAsync(factory, "docs-owner@example.com", Password);
        var html = await owner.GetStringAsync("/Services/MyDocuments");

        Assert.Contains("Passport", html);
        Assert.DoesNotContain("EmiratesId", html);
        Assert.Contains("/Services/DownloadDocument/", html);
        Assert.Contains("Upload Document", html);
    }

    [Fact]
    public async Task Upload_WritesToConfiguredUploadsDirectory()
    {
        using var factory = new TestAppFactory();
        await CreateUserAsync(factory, "storage-owner@example.com");

        var client = await HttpTestHelpers.LoginAsync(factory, "storage-owner@example.com", Password);
        var token = await HttpTestHelpers.GetTokenAsync(client, "/Services/Upload");

        var content = HttpTestHelpers.PdfUpload(string.Empty);
        content.Add(new StringContent(token), "__RequestVerificationToken");
        var response = await client.PostAsync("/Services/UploadDocument", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("File uploaded successfully", await response.Content.ReadAsStringAsync());
        Assert.Single(Directory.GetFiles(factory.UploadsPath, "*.pdf"));
    }

    private static async Task<string> CreateUserAsync(TestAppFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Flow",
            LastName = "Test",
            CreatedAt = DateTime.UtcNow
        };
        var result = await users.CreateAsync(user, Password);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
        return user.Id;
    }

    private static async Task<string> CreateApplicationAsync(TestAppFactory factory, string userId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var application = new ServiceApplication
        {
            UserId = userId,
            ServiceId = 1,
            Status = ApplicationStatus.Submitted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.ServiceApplications.Add(application);
        await db.SaveChangesAsync();

        db.ApplicationHistories.Add(new ApplicationHistory
        {
            ServiceApplicationId = application.Id,
            StatusState = ApplicationStatus.Submitted,
            Comments = "Application Submitted",
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return application.Id;
    }

    private static async Task<string> CreateDocumentAsync(
        TestAppFactory factory, string userId, string documentType)
    {
        var fileName = $"{Guid.NewGuid():N}.pdf";
        Directory.CreateDirectory(factory.UploadsPath);
        await File.WriteAllTextAsync(Path.Combine(factory.UploadsPath, fileName),
            "%PDF-1.4\n1 0 obj\n<<>>\nendobj\n%%EOF");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var document = new UserDocument
        {
            UserId = userId,
            DocumentType = documentType,
            FilePath = $"/uploads/{fileName}",
            UploadedAt = DateTime.UtcNow
        };
        db.UserDocuments.Add(document);
        await db.SaveChangesAsync();
        return document.Id;
    }
}
