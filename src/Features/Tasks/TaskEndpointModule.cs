using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskTrackerApi.Features.Tasks.Contracts;
using TaskTrackerApi.Features.Tasks.Services;
using TaskTrackerApi.Infrastructure.Endpoints;

namespace TaskTrackerApi.Features.Tasks;

public class TaskEndpointModule : EndpointModule
{
    public TaskEndpointModule() : base("/tasks")
    {
        HasApiVersion(1.0);
        WithName("Tasks");
        WithGroupName("Tasks");
        WithDescription("CRUD operations for tasks.");
        WithSummary("Tasks");
    }

    public override void MapEndpoints(IModuleEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("", GetTasks)
            .WithName("Get Tasks")
            .Produces<IReadOnlyList<TaskResponse>>(StatusCodes.Status200OK);

        endpoints.MapGet("{id:int}", GetTaskById)
            .WithName("Get Task By Id")
            .Produces<TaskResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        endpoints.MapPost("", CreateTask)
            .WithName("Create Task")
            .Accepts<CreateTaskRequest>("application/json")
            .Produces<TaskResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        endpoints.MapPut("{id:int}", UpdateTask)
            .WithName("Update Task")
            .Accepts<UpdateTaskRequest>("application/json")
            .Produces<TaskResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        endpoints.MapDelete("{id:int}", DeleteTask)
            .WithName("Delete Task")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetTasks(
        [FromServices] ITaskService taskService,
        CancellationToken cancellationToken)
    {
        var tasks = await taskService.GetAllAsync(cancellationToken);
        return Results.Ok(tasks);
    }

    private static async Task<IResult> GetTaskById(
        int id,
        [FromServices] ITaskService taskService,
        CancellationToken cancellationToken)
    {
        var task = await taskService.GetByIdAsync(id, cancellationToken);
        return task is null ? Results.NotFound() : Results.Ok(task);
    }

    private static async Task<IResult> CreateTask(
        [FromBody] CreateTaskRequest request,
        [FromServices] ITaskService taskService,
        [FromServices] ILogger<TaskEndpointModule> logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text) || string.IsNullOrWhiteSpace(request.Day))
        {
            return Results.BadRequest("Text and Day are required.");
        }

        try
        {
            var created = await taskService.CreateAsync(request, cancellationToken);
            return Results.Created($"/tasks/{created.Id}", created);
        }
        catch (DbUpdateException ex)
        {
            // A constraint/data violation is a client-data conflict, not an opaque server fault,
            // so surface a 409 here instead of letting it fall through to the generic 500 handler.
            logger.LogWarning(ex, "Failed to create task due to a database update conflict.");
            return Results.Problem(
                title: "The task could not be saved due to a data conflict.",
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<IResult> UpdateTask(
        int id,
        [FromBody] UpdateTaskRequest request,
        [FromServices] ITaskService taskService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text) || string.IsNullOrWhiteSpace(request.Day))
        {
            return Results.BadRequest("Text and Day are required.");
        }

        var updated = await taskService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? Results.NotFound() : Results.Ok(updated);
    }

    private static async Task<IResult> DeleteTask(
        int id,
        [FromServices] ITaskService taskService,
        CancellationToken cancellationToken)
    {
        var deleted = await taskService.DeleteAsync(id, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
