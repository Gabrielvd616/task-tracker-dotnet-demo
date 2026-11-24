using System.ComponentModel.DataAnnotations;

namespace TaskTrackerApi.Configuration;

public class AppSettings
{
    [Required]
    public string SqlServerDbConnectionString { get; set; } = string.Empty;
}
