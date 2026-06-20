# Solution Notes

**Candidate Name:** Pooja Kudale 
**Completion Date:** 20-June-2026

---

## Problems Identified

### 1. SQL Injection

Biggest issue. `TodoService.cs` was building all SQL queries with string interpolation directly:

```csharp
command.CommandText = $"SELECT * FROM Todos WHERE Id = {id}";
command.CommandText = $"INSERT INTO Todos ... VALUES ('{todo.Title}', '{todo.Description}' ...)";
```

Anyone could pass `'; DROP TABLE Todos; --` as a title and it would execute. Fixed all four queries to use parameterized queries with `$param` syntax.

### 2. No dependency injection

Every action method was manually creating a new service instance:

```csharp
public IActionResult CreateTodo([FromBody] Todo todo)
{
    var todoService = new TodoService();
    ...
}
```

This was repeated in all four action methods. The DI container was completely bypassed, the controller was tightly coupled to the concrete class, and it was impossible to test without hitting the real database.

### 3. No interface for the service

No `ITodoService` existed. Without it you can't mock the service in unit tests and the controller directly depends on the implementation. Added the interface and had `TodoService` implement it.

### 4. Hardcoded connection string

`TodoService` had `private string _connectionString = "Data Source=todos.db"` baked into the class. Should come from `IConfiguration`. Changed the constructor to accept `IConfiguration` and read from it, with a fallback.

### 5. Null crash on Description reads

The DB schema has `Description TEXT` (nullable), but reads were:

```csharp
Description = reader.GetString(2)
```

This throws if the column is null. Added a null check: `reader.IsDBNull(2) ? null : reader.GetString(2)`.

### 6. Wrong HTTP verbs and broken REST routes

Original routes were:
```
POST /api/createTodo
POST /api/getTodo
POST /api/updateTodo
POST /api/deleteTodo
```

Using POST for everything is wrong. Verbs in route names is an anti-pattern - routes should identify resources. GET was reading an ID from the request body which doesn't make sense.

Fixed to proper REST:
```
POST   /api/todos
GET    /api/todos
GET    /api/todos/{id}
PUT    /api/todos/{id}
DELETE /api/todos/{id}
```

### 7. No model validation

`Title` is `NOT NULL` in the database but had no `[Required]` attribute. A request with no title would fail with a generic database error rather than a clean 400 Bad Request.

### 8. Wrong status code on create

`CreateTodo` was returning `200 OK`. Creating a resource should return `201 Created` with a `Location` header pointing to where you can get it back.

---

## Changes Made

### New file: `Services/ITodoService.cs`

Created the interface with all five methods. The controller now depends on this abstraction instead of the concrete class.

### `Services/TodoService.cs`

- Implements `ITodoService`
- Constructor takes `IConfiguration`, reads connection string from `ConnectionStrings:DefaultConnection`
- All SQL queries rewritten to use parameterized queries
- Description reads safely with `IsDBNull` check
- `GetTodoById` returns `Todo?` - explicit that null is a valid return value

### `Controllers/TodoController.cs`

- `ITodoService` injected via constructor, stored as `readonly` field
- All `new TodoService()` calls removed
- Routes fixed to proper REST conventions under `api/todos`
- `CreateTodo` returns `201 Created` via `CreatedAtAction`
- GET endpoint uses `{id}` path param instead of reading from body
- Removed `GetTodoRequest` and `DeleteTodoRequest` - pointless wrapper classes for a single int
- `UpdateTodoRequest` no longer has an `Id` field - it comes from the route
- Split the old single `GetTodo` method into `GetAllTodos` and `GetTodoById`

### `Models/Todo.cs`

- `[Required]` added to `Title`
- `Description` changed to `string?`
- `Title` initialized to `string.Empty`

### `Program.cs`

- Added `builder.Services.AddScoped<ITodoService, TodoService>()` to register the service
- `InitializeDatabase` takes `IConfiguration` now and reads the connection string from config

### `appsettings.json`

Added:
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=todos.db"
}
```

[Your decisions here]

---

## Architecture Decisions

**Why `AddScoped` instead of `AddSingleton` for `TodoService`?**

Scoped means one instance per HTTP request. Singleton would be one shared instance for the lifetime of the app. Since `TodoService` opens a new `SqliteConnection` per method call, either would technically work here, but Scoped is the safer default for anything that might hold per-request state.

**Why keep `UpdateTodoRequest` separate from `Todo`?**

The `Todo` model has `Id` and `CreatedAt` fields that shouldn't come from user input on an update. Having a separate request class means we only accept what we actually want to change.

**Why raw ADO.NET instead of EF Core?**

The project already used raw SQLite with `Microsoft.Data.Sqlite`. Swapping to EF Core would have been a bigger change than what was asked for. The existing approach works fine for a simple CRUD API, I just fixed the bugs in it.

---

## Testing

The original `UnitTest1.cs` had a bunch of problems:
- Tests depended on each other's side effects (`TestGetTodo` expected records already in the DB from `TestCreateTodo`)
- `UpdateTest` hardcoded `id = 1` which doesn't always exist
- No controller tests for GET, PUT, DELETE
- Tests shared the same database with no cleanup
- One test was literally `Assert.True(true)`

Deleted it and split into two files.

### `TodoServiceTests.cs`

Integration tests against a real SQLite database. Each test gets its own database via a GUID-based filename - xUnit creates a new class instance per test method so there's no shared state at all. `IDisposable` cleans up the file after each test. Had to call `SqliteConnection.ClearAllPools()` before deleting because SQLite's connection pool keeps a handle open otherwise and the file delete fails with an IOException.

Covers: create, create with null description, get all, get all empty, get by id, get by id not found, update, delete found, delete not found.

### `TodoControllerTests.cs`

Pure unit tests using Moq to mock `ITodoService`. No database, no I/O. Tests that the controller calls the right methods and returns the right HTTP status codes.

Covers every endpoint for both success and not-found cases.

Added `Moq` and `Microsoft.Data.Sqlite` packages to the test project.

---

## API Endpoints

Base URL: `http://localhost:5164`

#### Create TODO
```
POST /api/todos
Content-Type: application/json

{
  "title": "Buy groceries",
  "description": "milk, eggs",
  "isCompleted": false
}

### Endpoints

#### Get all TODOs
```
GET /api/todos

Response: 200 OK
[ { ... }, { ... } ]
```

#### Get a TODO by ID
```
GET /api/todos/1

Response: 200 OK or 404 Not Found
```

#### Update TODO
```
PUT /api/todos/1
Content-Type: application/json

{
  "title": "Updated title",
  "description": "updated desc",
  "isCompleted": true
}

Response: 200 OK or 404 Not Found
```

#### Delete TODO
```
DELETE /api/todos/1

Response: 200 OK or 404 Not Found
```

---

## How to Run

```bash
cd TodoApi
dotnet run
```

Swagger UI: `http://localhost:5164/swagger`

```bash
cd TodoApi.Tests
dotnet test
```

---

## What I'd Do With More Time

- **Make everything async** - all service methods are synchronous right now. For a real API they should be `async Task<T>` with proper `await` on DB calls
- **Move `InitializeDatabase` out of Program.cs** - it's a bit messy sitting there as a local function, would be cleaner as a dedicated class
- **Global error handling** - the controller catches generic `Exception` everywhere and returns `BadRequest` with the exception message. Should use middleware for that and return proper problem details
- **Move request models to their own folder** - `UpdateTodoRequest` is currently at the bottom of the controller file
- **Pagination on GetAllTodos** - no limit on how many records get returned right now
