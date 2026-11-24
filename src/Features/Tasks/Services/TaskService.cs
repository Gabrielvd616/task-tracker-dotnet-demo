using Microsoft.EntityFrameworkCore;
using TaskTrackerApi.Domain;
using TaskTrackerApi.Features.Tasks.Contracts;
using TaskTrackerApi.Infrastructure.Data;

namespace TaskTrackerApi.Features.Tasks.Services;

public class TaskService : ITaskService
{
    private readonly TaskDbContext _dbContext;

    public TaskService(TaskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Tasks
            .AsNoTracking()
            .OrderBy(task => task.Id)
            .Select(task => ToResponse(task))
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(task => task.Id == id, cancellationToken);

        return task is null ? null : ToResponse(task);
    }

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = new TaskItem
        {
            Text = request.Text,
            Day = request.Day,
            Reminder = request.Reminder
        };

        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(task);
    }

    public async Task<TaskResponse?> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks.FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
        if (task is null)
        {
            return null;
        }

        task.Text = request.Text;
        task.Day = request.Day;
        task.Reminder = request.Reminder;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(task);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks.FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
        if (task is null)
        {
            return false;
        }

        _dbContext.Tasks.Remove(task);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static TaskResponse ToResponse(TaskItem task)
        => new(task.Id, task.Text, task.Day, task.Reminder);
}
