using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using TaskTrackerApi.Domain;

namespace TaskTrackerApi.Infrastructure.Data;

[ExcludeFromCodeCoverage]
public class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options)
    {
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("Task");
            entity.HasKey(task => task.Id);

            // Id is a database-generated identity primary key per the deployment script.
            entity.Property(task => task.Id).ValueGeneratedOnAdd();
            entity.Property(task => task.Text).HasMaxLength(255).IsRequired();
            entity.Property(task => task.Day).HasMaxLength(32).IsRequired();
            entity.Property(task => task.Reminder).IsRequired();
        });
    }
}
