namespace TaskTrackerApi.Features.Tasks.Contracts;

public record CreateTaskRequest(string Text, string Day, bool Reminder);
