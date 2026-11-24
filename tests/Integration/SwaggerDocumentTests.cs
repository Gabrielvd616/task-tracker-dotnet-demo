using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace TaskTrackerApi.IntegrationTests;

public class SwaggerDocumentTests
{
    [Fact]
    public async Task SwaggerDocument_ExposesAllTaskOperations()
    {
        using var factory = new TaskApiWebApplicationFactory();
        var client = factory.CreateClient();

        var json = await client.GetStringAsync("/swagger/v1/swagger.json");

        using var document = JsonDocument.Parse(json);
        var paths = document.RootElement.GetProperty("paths");

        paths.TryGetProperty("/tasks", out var tasks).Should().BeTrue();
        tasks.TryGetProperty("get", out _).Should().BeTrue();
        tasks.TryGetProperty("post", out _).Should().BeTrue();

        paths.TryGetProperty("/tasks/{id}", out var taskById).Should().BeTrue();
        taskById.TryGetProperty("get", out _).Should().BeTrue();
        taskById.TryGetProperty("put", out _).Should().BeTrue();
        taskById.TryGetProperty("delete", out _).Should().BeTrue();
    }
}
