using Bogus;
using FluentAssertions;
using TaskTrackerApi.Domain;
using TaskTrackerApi.Features.Tasks.Contracts;
using TaskTrackerApi.Features.Tasks.Services;
using TaskTrackerApi.UnitTests.Helpers;
using Xunit;

namespace TaskTrackerApi.UnitTests.Features.Tasks.Services;

public class TaskServiceTests
{
    private const string DayFormat = "yyyy-MM-dd'T'HH:mm";

    private static TaskItem NewTaskEntity(Faker faker) => new()
    {
        // Id is left unset so the database assigns the identity value.
        Text = faker.Lorem.Sentence(3),
        Day = faker.Date.Soon().ToString(DayFormat),
        Reminder = faker.Random.Bool()
    };

    private static CreateTaskRequest NewCreateRequest(Faker faker) =>
        new(faker.Lorem.Sentence(3), faker.Date.Soon().ToString(DayFormat), faker.Random.Bool());

    private static UpdateTaskRequest NewUpdateRequest(Faker faker) =>
        new(faker.Lorem.Sentence(3), faker.Date.Soon().ToString(DayFormat), faker.Random.Bool());

    private static IReadOnlyList<TaskItem> Seed(SqliteInMemoryDbContext db, Faker faker, int count = 4)
    {
        var tasks = Enumerable.Range(0, count).Select(_ => NewTaskEntity(faker)).ToList();
        db.Context.Tasks.AddRange(tasks);
        db.Context.SaveChanges();
        return tasks;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllTasks_OrderedById()
    {
        var faker = new Faker();
        using var db = new SqliteInMemoryDbContext();
        var seeded = Seed(db, faker);
        var service = new TaskService(db.Context);

        var result = await service.GetAllAsync(CancellationToken.None);

        result.Should().HaveCount(seeded.Count);
        result.Select(task => task.Id).Should().BeInAscendingOrder();
        result.Select(task => task.Text).Should().BeEquivalentTo(seeded.Select(task => task.Text));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsTask()
    {
        var faker = new Faker();
        using var db = new SqliteInMemoryDbContext();
        var seeded = Seed(db, faker);
        var expected = seeded[faker.Random.Int(0, seeded.Count - 1)];
        var service = new TaskService(db.Context);

        var result = await service.GetByIdAsync(expected.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Text.Should().Be(expected.Text);
        result.Day.Should().Be(expected.Day);
        result.Reminder.Should().Be(expected.Reminder);
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ReturnsNull()
    {
        var faker = new Faker();
        using var db = new SqliteInMemoryDbContext();
        var seeded = Seed(db, faker);
        var missingId = seeded.Max(task => task.Id) + faker.Random.Int(1, 100);
        var service = new TaskService(db.Context);

        var result = await service.GetByIdAsync(missingId, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_PersistsTask_AndAssignsGeneratedId()
    {
        var faker = new Faker();
        using var db = new SqliteInMemoryDbContext();
        var seeded = Seed(db, faker);
        var request = NewCreateRequest(faker);
        var service = new TaskService(db.Context);

        var created = await service.CreateAsync(request, CancellationToken.None);

        created.Id.Should().BeGreaterThan(seeded.Max(task => task.Id));
        created.Text.Should().Be(request.Text);
        created.Day.Should().Be(request.Day);
        created.Reminder.Should().Be(request.Reminder);

        await using var verifyContext = db.NewContext();
        var persisted = await verifyContext.FindAsync<TaskItem>(created.Id);
        persisted.Should().NotBeNull();
        persisted!.Text.Should().Be(request.Text);
    }

    [Fact]
    public async Task CreateAsync_EmptyTable_StartsAtIdOne()
    {
        var faker = new Faker();
        using var db = new SqliteInMemoryDbContext();
        var request = NewCreateRequest(faker);
        var service = new TaskService(db.Context);

        var created = await service.CreateAsync(request, CancellationToken.None);

        created.Id.Should().Be(1);
        created.Text.Should().Be(request.Text);
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesFields()
    {
        var faker = new Faker();
        using var db = new SqliteInMemoryDbContext();
        var seeded = Seed(db, faker);
        var target = seeded[faker.Random.Int(0, seeded.Count - 1)];
        var request = NewUpdateRequest(faker);
        var service = new TaskService(db.Context);

        var updated = await service.UpdateAsync(target.Id, request, CancellationToken.None);

        updated.Should().NotBeNull();
        updated!.Text.Should().Be(request.Text);
        updated.Day.Should().Be(request.Day);
        updated.Reminder.Should().Be(request.Reminder);

        await using var verifyContext = db.NewContext();
        var persisted = await verifyContext.FindAsync<TaskItem>(target.Id);
        persisted!.Text.Should().Be(request.Text);
    }

    [Fact]
    public async Task UpdateAsync_MissingId_ReturnsNull()
    {
        var faker = new Faker();
        using var db = new SqliteInMemoryDbContext();
        var seeded = Seed(db, faker);
        var missingId = seeded.Max(task => task.Id) + faker.Random.Int(1, 100);
        var service = new TaskService(db.Context);

        var updated = await service.UpdateAsync(missingId, NewUpdateRequest(faker), CancellationToken.None);

        updated.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesTask_AndReturnsTrue()
    {
        var faker = new Faker();
        using var db = new SqliteInMemoryDbContext();
        var seeded = Seed(db, faker);
        var target = seeded[faker.Random.Int(0, seeded.Count - 1)];
        var service = new TaskService(db.Context);

        var deleted = await service.DeleteAsync(target.Id, CancellationToken.None);

        deleted.Should().BeTrue();

        await using var verifyContext = db.NewContext();
        (await verifyContext.FindAsync<TaskItem>(target.Id)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_MissingId_ReturnsFalse()
    {
        var faker = new Faker();
        using var db = new SqliteInMemoryDbContext();
        var seeded = Seed(db, faker);
        var missingId = seeded.Max(task => task.Id) + faker.Random.Int(1, 100);
        var service = new TaskService(db.Context);

        var deleted = await service.DeleteAsync(missingId, CancellationToken.None);

        deleted.Should().BeFalse();
    }
}
