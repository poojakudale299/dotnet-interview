using Microsoft.AspNetCore.Mvc;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Controllers
{
    [ApiController]
    [Route("todos")]
    public class TodoController : ControllerBase
    {
        private readonly ITodoService _todoService;

        public TodoController(ITodoService todoService)
        {
            _todoService = todoService;
        }

        [HttpPost]
        public IActionResult CreateTodo([FromBody] Todo todo)
        {
            try
            {
                var result = _todoService.CreateTodo(todo);
                return CreatedAtAction(nameof(GetTodoById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public IActionResult GetAllTodos()
        {
            try
            {
                var todos = _todoService.GetAllTodos();
                return Ok(todos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetTodoById(int id)
        {
            try
            {
                var todo = _todoService.GetTodoById(id);
                if (todo == null)
                    return NotFound();
                return Ok(todo);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public IActionResult UpdateTodo(int id, [FromBody] UpdateTodoRequest request)
        {
            try
            {
                var existingTodo = _todoService.GetTodoById(id);
                if (existingTodo == null)
                    return NotFound();

                var todo = new Todo
                {
                    Title = request.Title,
                    Description = request.Description,
                    IsCompleted = request.IsCompleted
                };

                var result = _todoService.UpdateTodo(id, todo);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteTodo(int id)
        {
            try
            {
                var result = _todoService.DeleteTodo(id);
                if (result)
                    return Ok(new { message = "Todo deleted successfully" });
                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    public class UpdateTodoRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsCompleted { get; set; }
    }
}
