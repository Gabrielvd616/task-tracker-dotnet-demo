namespace TaskTrackerApi.Features.Tasks.Contracts;

public record TaskResponse(int Id, string Text, string Day, bool Reminder);
