namespace TaskTrackerApi.Features.Tasks.Contracts;

public record UpdateTaskRequest(string Text, string Day, bool Reminder);
