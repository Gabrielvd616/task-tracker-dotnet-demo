using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskTrackerApi.Features.Tasks.Contracts;
using TaskTrackerApi.Features.Tasks.Services;
using Xunit;

namespace TaskTrackerApi.IntegrationTests;

public class TaskEndpointsIntegrationTests
{
    private static readonly object ValidTaskPayload =
        new { Text = "Buy milk", Day = "2026-11-05T10:00", Reminder = true };

    [Fact]
    public async Task GetTasks_ReturnsOkWithData()
    {
        using var factory = new TaskApiWebApplicationFactory();
        factory.TaskServiceMock
            .Setup(service => service.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TaskResponse> { new(1, "Buy milk", "2026-11-05T10:00", true) });
        var client = factory.CreateClient();

        var tasks = await client.GetFromJsonAsync<List<TaskResponse>>("/tasks");

        tasks.Should().NotBeNull();
        tasks!.Should().ContainSingle(task => task.Text == "Buy milk");
    }

    [Fact]
    public async Task CreateTask_WithInvalidBody_Returns400_AndDoesNotCallService()
    {
        using var factory = new TaskApiWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/tasks", new { Text = "", Day = "", Reminder = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        factory.TaskServiceMock.Verify(
            service => service.CreateAsync(It.IsAny<CreateTaskRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateTask_WhenServiceThrowsDbUpdateException_Returns409ProblemDetails()
    {
        using var factory = new TaskApiWebApplicationFactory();
        factory.TaskServiceMock
            .Setup(service => service.CreateAsync(It.IsAny<CreateTaskRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("unique constraint violation"));
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/tasks", ValidTaskPayload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task CreateTask_WhenServiceThrowsUnexpectedException_Returns500_WithoutLeakingDetails()
    {
        using var factory = new TaskApiWebApplicationFactory();
        factory.TaskServiceMock
            .Setup(service => service.CreateAsync(It.IsAny<CreateTaskRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive-connection-string-details"));
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/tasks", ValidTaskPayload);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("sensitive-connection-string-details");
    }
}
