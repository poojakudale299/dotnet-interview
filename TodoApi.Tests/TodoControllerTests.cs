using Microsoft.AspNetCore.Mvc;
using Moq;
using TodoApi.Controllers;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Tests;

public class TodoControllerTests
{
    private readonly Mock<ITodoService> _mockService;
    private readonly TodoController _controller;

    public TodoControllerTests()
    {
        _mockService = new Mock<ITodoService>();
        _controller = new TodoController(_mockService.Object);
    }

    [Fact]
    public void CreateTodo_Returns201_WithCreatedTodo()
    {
        var input = new Todo { Title = "Test" };
        var saved = new Todo { Id = 1, Title = "Test", CreatedAt = DateTime.UtcNow };
        _mockService.Setup(s => s.CreateTodo(input)).Returns(saved);

        var result = _controller.CreateTodo(input);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(saved, created.Value);
    }

    [Fact]
    public void GetAllTodos_Returns200_WithList()
    {
        var todos = new List<Todo>
        {
            new() { Id = 1, Title = "A" },
            new() { Id = 2, Title = "B" }
        };
        _mockService.Setup(s => s.GetAllTodos()).Returns(todos);

        var result = _controller.GetAllTodos();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(todos, ok.Value);
    }

    [Fact]
    public void GetTodoById_Returns200_WhenFound()
    {
        var todo = new Todo { Id = 1, Title = "Existing" };
        _mockService.Setup(s => s.GetTodoById(1)).Returns(todo);

        var result = _controller.GetTodoById(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(todo, ok.Value);
    }

    [Fact]
    public void GetTodoById_Returns404_WhenNotFound()
    {
        _mockService.Setup(s => s.GetTodoById(99)).Returns((Todo?)null);

        var result = _controller.GetTodoById(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void UpdateTodo_Returns200_WhenUpdated()
    {
        var existing = new Todo { Id = 1, Title = "Old" };
        var request = new UpdateTodoRequest { Title = "New", Description = "Desc", IsCompleted = true };
        var updated = new Todo { Id = 1, Title = "New", IsCompleted = true };
        _mockService.Setup(s => s.GetTodoById(1)).Returns(existing);
        _mockService.Setup(s => s.UpdateTodo(1, It.IsAny<Todo>())).Returns(updated);

        var result = _controller.UpdateTodo(1, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(updated, ok.Value);
    }

    [Fact]
    public void UpdateTodo_Returns404_WhenNotFound()
    {
        _mockService.Setup(s => s.GetTodoById(99)).Returns((Todo?)null);

        var result = _controller.UpdateTodo(99, new UpdateTodoRequest { Title = "X" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void DeleteTodo_Returns200_WhenDeleted()
    {
        _mockService.Setup(s => s.DeleteTodo(1)).Returns(true);

        var result = _controller.DeleteTodo(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void DeleteTodo_Returns404_WhenNotFound()
    {
        _mockService.Setup(s => s.DeleteTodo(99)).Returns(false);

        var result = _controller.DeleteTodo(99);

        Assert.IsType<NotFoundResult>(result);
    }
}
