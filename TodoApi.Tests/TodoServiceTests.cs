using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Tests;

public class TodoServiceTests : IDisposable
{
    private readonly TodoService _service;
    private readonly string _dbPath;

    public TodoServiceTests()
    {
        _dbPath = $"test_{Guid.NewGuid():N}.db";

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}" }
            })
            .Build();

        InitDb();
        _service = new TodoService(config);
    }

    private void InitDb()
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Todos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Description TEXT,
                IsCompleted INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public void CreateTodo_ReturnsTodoWithId()
    {
        var todo = new Todo { Title = "Buy milk", Description = "2% milk" };

        var result = _service.CreateTodo(todo);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Buy milk", result.Title);
    }

    [Fact]
    public void CreateTodo_WithNullDescription_ShouldNotThrow()
    {
        var todo = new Todo { Title = "No description" };

        var result = _service.CreateTodo(todo);

        Assert.NotNull(result);
        Assert.Null(result.Description);
    }

    [Fact]
    public void GetAllTodos_ReturnsAllCreatedTodos()
    {
        _service.CreateTodo(new Todo { Title = "Task 1" });
        _service.CreateTodo(new Todo { Title = "Task 2" });

        var todos = _service.GetAllTodos();

        Assert.Equal(2, todos.Count);
    }

    [Fact]
    public void GetAllTodos_ReturnsEmpty_WhenNoTodos()
    {
        var todos = _service.GetAllTodos();

        Assert.Empty(todos);
    }

    [Fact]
    public void GetTodoById_ReturnsCorrectTodo()
    {
        var created = _service.CreateTodo(new Todo { Title = "Find me" });

        var result = _service.GetTodoById(created.Id);

        Assert.NotNull(result);
        Assert.Equal("Find me", result.Title);
    }

    [Fact]
    public void GetTodoById_ReturnsNull_WhenNotFound()
    {
        var result = _service.GetTodoById(9999);

        Assert.Null(result);
    }

    [Fact]
    public void UpdateTodo_UpdatesAllFields()
    {
        var created = _service.CreateTodo(new Todo { Title = "Original" });
        var updated = new Todo { Title = "Updated", Description = "New desc", IsCompleted = true };

        var result = _service.UpdateTodo(created.Id, updated);

        Assert.Equal("Updated", result.Title);
        Assert.Equal("New desc", result.Description);
        Assert.True(result.IsCompleted);
    }

    [Fact]
    public void DeleteTodo_ReturnsTrue_WhenExists()
    {
        var created = _service.CreateTodo(new Todo { Title = "Delete me" });

        var deleted = _service.DeleteTodo(created.Id);

        Assert.True(deleted);
        Assert.Null(_service.GetTodoById(created.Id));
    }

    [Fact]
    public void DeleteTodo_ReturnsFalse_WhenNotFound()
    {
        var result = _service.DeleteTodo(9999);

        Assert.False(result);
    }
}
