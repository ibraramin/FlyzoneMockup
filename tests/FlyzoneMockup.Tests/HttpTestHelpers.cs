using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FlyzoneMockup.Tests;

internal static class HttpTestHelpers
{
    public static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    /// <summary>Fetches a page with an existing (authenticated) client and reads its token.</summary>
    public static async Task<string> GetTokenAsync(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        var match = Regex.Match(html,
            @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!match.Success)
        {
            match = Regex.Match(html,
                @"<input[^>]*value=""([^""]+)""[^>]*name=""__RequestVerificationToken""",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        Assert.True(match.Success, $"No antiforgery token found on {path}");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    /// <summary>Fetches a page with a fresh client and extracts token plus cookie.</summary>
    public static async Task<(HttpClient Client, string Token)> GetFormAsync(
        WebApplicationFactory<Program> factory, string path)
    {
        var client = CreateClient(factory);
        return (client, await GetTokenAsync(client, path));
    }

    public static FormUrlEncodedContent Form(params (string Key, string Value)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)));

    public static async Task<HttpClient> LoginAsync(
        WebApplicationFactory<Program> factory, string email, string password)
    {
        var (client, token) = await GetFormAsync(factory, "/Account/Login");
        var response = await client.PostAsync("/Account/Login", Form(
            ("Email", email),
            ("Password", password),
            ("RememberMe", "false"),
            ("__RequestVerificationToken", token)));

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/Account/Dashboard", response.Headers.Location?.ToString() ?? string.Empty);
        return client;
    }

    public static MultipartFormDataContent PdfUpload(string applicationId, string documentType = "Passport")
    {
        var content = new MultipartFormDataContent();
        var bytes = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<<>>\nendobj\n%%EOF");
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(file, "file", "passport.pdf");
        content.Add(new StringContent(documentType), "documentType");
        content.Add(new StringContent(applicationId), "applicationId");
        return content;
    }
}
