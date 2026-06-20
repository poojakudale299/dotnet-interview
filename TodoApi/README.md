# TODO API

A simple TODO API built with ASP.NET Core 8.0.

## Getting Started

### Prerequisites
- .NET 8.0 SDK

### Running the Application

1. Navigate to the TodoApi directory:
```
cd TodoApi
```

2. Run the application:
```
dotnet run
```

3. The API will be available at `http://localhost:5164` (or check the console output for the exact URL)

4. Access Swagger UI at `http://localhost:5164/swagger` to test the endpoints

## API Endpoints

All endpoints are under `/api/todos`:

- `POST /api/todos` - Create a new TODO item
- `GET /api/todos` - Get all TODO items
- `GET /api/todos/{id}` - Get a TODO item by ID
- `PUT /api/todos/{id}` - Update a TODO item
- `DELETE /api/todos/{id}` - Delete a TODO item

## Testing

Run the tests with:
```
cd TodoApi.Tests
dotnet test
```

## Database

The application uses SQLite with a file-based database (`todos.db`) that is automatically created on startup.