using System.Net;

namespace FlyzoneMockup.Tests;

public class BasicPagesTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public BasicPagesTests(TestAppFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/")]
    [InlineData("/Account/Login")]
    [InlineData("/Account/Register")]
    public async Task PublicPages_ReturnOk(string path)
    {
        var client = HttpTestHelpers.CreateClient(_factory);
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_RequiresAuthentication()
    {
        var client = HttpTestHelpers.CreateClient(_factory);
        var response = await client.GetAsync("/Account/Dashboard");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("Account/Login", response.Headers.Location?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task Upload_RequiresAuthentication()
    {
        var client = HttpTestHelpers.CreateClient(_factory);
        var response = await client.PostAsync("/Services/UploadDocument",
            new MultipartFormDataContent());
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("Account/Login", response.Headers.Location?.ToString() ?? string.Empty);
    }
}
