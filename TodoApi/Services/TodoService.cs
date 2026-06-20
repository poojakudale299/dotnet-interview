using Microsoft.Data.Sqlite;
using TodoApi.Models;

namespace TodoApi.Services
{
    public class TodoService : ITodoService
    {
        private readonly string _connectionString;

        public TodoService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=todos.db";
        }

        public Todo CreateTodo(Todo todo)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Todos (Title, Description, IsCompleted, CreatedAt)
                VALUES ($title, $description, $isCompleted, $createdAt);
                SELECT last_insert_rowid();
            ";
            command.Parameters.AddWithValue("$title", todo.Title);
            command.Parameters.AddWithValue("$description", (object?)todo.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("$isCompleted", todo.IsCompleted ? 1 : 0);
            command.Parameters.AddWithValue("$createdAt", DateTime.UtcNow.ToString("o"));

            var id = Convert.ToInt32(command.ExecuteScalar());
            todo.Id = id;
            todo.CreatedAt = DateTime.UtcNow;
            return todo;
        }

        public List<Todo> GetAllTodos()
        {
            var todos = new List<Todo>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Todos";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                todos.Add(new Todo
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    IsCompleted = reader.GetInt32(3) == 1,
                    CreatedAt = DateTime.Parse(reader.GetString(4))
                });
            }

            return todos;
        }

        public Todo? GetTodoById(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Todos WHERE Id = $id";
            command.Parameters.AddWithValue("$id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Todo
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    IsCompleted = reader.GetInt32(3) == 1,
                    CreatedAt = DateTime.Parse(reader.GetString(4))
                };
            }

            return null;
        }

        public Todo UpdateTodo(int id, Todo todo)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Todos
                SET Title = $title, Description = $description, IsCompleted = $isCompleted
                WHERE Id = $id
            ";
            command.Parameters.AddWithValue("$title", todo.Title);
            command.Parameters.AddWithValue("$description", (object?)todo.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("$isCompleted", todo.IsCompleted ? 1 : 0);
            command.Parameters.AddWithValue("$id", id);

            command.ExecuteNonQuery();

            todo.Id = id;
            return todo;
        }

        public bool DeleteTodo(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Todos WHERE Id = $id";
            command.Parameters.AddWithValue("$id", id);

            var rowsAffected = command.ExecuteNonQuery();
            return rowsAffected > 0;
        }
    }
}
