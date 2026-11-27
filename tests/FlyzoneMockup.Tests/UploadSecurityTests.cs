using System.Net;
using Flyzone.Data;
using Flyzone.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlyzoneMockup.Tests;

public class UploadSecurityTests : IClassFixture<TestAppFactory>
{
    private const string Password = "Test@1234";
    private readonly TestAppFactory _factory;

    public UploadSecurityTests(TestAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Upload_RejectsFileWithWrongMagicBytes()
    {
        await CreateUserAsync("uploader@example.com");
        var client = await HttpTestHelpers.LoginAsync(_factory, "uploader@example.com", Password);
        var token = await HttpTestHelpers.GetTokenAsync(client, "/Services/GoldenVisa");

        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(System.Text.Encoding.ASCII.GetBytes("this is not a pdf"));
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(file, "file", "fake.pdf");
        content.Add(new StringContent("Passport"), "documentType");
        content.Add(new StringContent(token), "__RequestVerificationToken");

        var response = await client.PostAsync("/Services/UploadDocument", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid file content", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Upload_RejectsAttachingDocumentToAnotherUsersApplication()
    {
        var ownerId = await CreateUserAsync("owner@example.com");
        await CreateUserAsync("intruder@example.com");

        string foreignApplicationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var application = new ServiceApplication
            {
                UserId = ownerId,
                ServiceId = 1,
                Status = ApplicationStatus.Submitted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.ServiceApplications.Add(application);
            await db.SaveChangesAsync();
            foreignApplicationId = application.Id;
        }

        var client = await HttpTestHelpers.LoginAsync(_factory, "intruder@example.com", Password);
        var token = await HttpTestHelpers.GetTokenAsync(client, "/Services/GoldenVisa");

        var content = HttpTestHelpers.PdfUpload(foreignApplicationId);
        content.Add(new StringContent(token), "__RequestVerificationToken");

        var response = await client.PostAsync("/Services/UploadDocument", content);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var queryScope = _factory.Services.CreateScope();
        var queryDb = queryScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await queryDb.UserDocuments
            .AnyAsync(d => d.ServiceApplicationId == foreignApplicationId));
    }

    private async Task<string> CreateUserAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow
        };
        var result = await users.CreateAsync(user, Password);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
        return user.Id;
    }
}
