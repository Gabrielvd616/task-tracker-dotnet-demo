using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Moq;
using TaskTrackerApi.Features.Tasks.Contracts;
using TaskTrackerApi.Features.Tasks.Services;
using Xunit;

namespace TaskTrackerApi.IntegrationTests;

public class CorsPolicyTests
{
    [Theory]
    [InlineData("http://localhost:4200")]
    [InlineData("https://goldfish-app-zhi87.ondigitalocean.app")]
    public async Task Preflight_FromAllowedOrigin_IsNotRedirected_AndEchoesOrigin(string origin)
    {
        using var factory = new TaskApiWebApplicationFactory();
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Options, "/tasks");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type, authorization");

        var response = await client.SendAsync(request);

        // Preflight must be answered by CORS (2xx), not 307-redirected by HTTPS redirection.
        ((int)response.StatusCode).Should().BeLessThan(300);
        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var allowOrigin).Should().BeTrue();
        allowOrigin!.Should().Contain(origin);
    }

    [Fact]
    public async Task Preflight_FromDisallowedOrigin_DoesNotEchoOrigin()
    {
        using var factory = new TaskApiWebApplicationFactory();
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Options, "/tasks");
        request.Headers.Add("Origin", "https://evil.example.com");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task ErrorResponse_FromAllowedOrigin_StillIncludesAllowOriginHeader()
    {
        using var factory = new TaskApiWebApplicationFactory();
        factory.TaskServiceMock
            .Setup(service => service.CreateAsync(It.IsAny<CreateTaskRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/tasks")
        {
            Content = JsonContent.Create(new { Text = "x", Day = "2026-11-05T10:00", Reminder = true })
        };
        request.Headers.Add("Origin", "http://localhost:4200");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var allowOrigin).Should().BeTrue();
        allowOrigin!.Should().Contain("http://localhost:4200");
    }
}
