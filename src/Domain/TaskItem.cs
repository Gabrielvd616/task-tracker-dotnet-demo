namespace TaskTrackerApi.Domain;

/// <summary>
/// Task entity mapped to the dbo.Task table. Named TaskItem to avoid clashing with
/// System.Threading.Tasks.Task used throughout the async API surface.
/// </summary>
public class TaskItem
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public bool Reminder { get; set; }
}
