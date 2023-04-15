using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

class Program
{
    public static List<Todo> _Todos = new List<Todo>
    {
        new Todo { Name = "Make breakfast" },
        new Todo { Name = "Study GraphQL" }
    };

    static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddScoped<ITodoService>(
                ts => new TodoService(new TodoDataStore()))
            .AddDbContext<AppContext>()
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>();

        var app = builder.Build();

        app.MapGraphQL();

        app.MapGet("/todos", (ITodoService todoService) =>
        {
            return todoService.GetTodos();
        });

        app.Run();
    }
}

public class Todo
{
    public string Name { get; set; } = "";
    public Status Status { get; set; }
}

public enum Status
{
    Incomplete = 0,
    InProgress,
    Done
}

public class Query
{
    public List<Todo> GetTodos()
    {
        var todos = new TodoDataStore();
        return todos.GetTodos().Result;
    }
}

public class Mutation
{
    public Todo AddTodo(string name)
    {
        var todos = new TodoDataStore();
        return todos.AddTodo(name).Result;
    }
}

public class TodoService : ITodoService
{
    private ITodoDataStore TodoDataStore { get; set; }
    public TodoService(ITodoDataStore todoDataStore)
    {
        TodoDataStore = todoDataStore;
    }

    public IResult GetTodos()
    {
        var todos = TodoDataStore.GetTodos().Result;
        return Results.Ok(todos);
    }

    public IResult AddTodo(string name)
    {
        var todo = TodoDataStore.AddTodo(name).Result;
        return Results.Created("", todo);
    }
}

public interface ITodoService
{
    public IResult GetTodos();
    public IResult AddTodo(string name);
}

public class MockTodoDataStore : ITodoDataStore
{
    public Task<List<Todo>> GetTodos()
    {
        return Task.FromResult(Program._Todos);
    }

    public Task<Todo> AddTodo(string name)
    {
        var todo = new Todo { Name = name };
        Program._Todos.Add(todo);
        return Task.FromResult(todo);
    }
}

public class TodoDataStore : ITodoDataStore
{
    public async Task<List<Todo>> GetTodos()
    {
        using var appContext = new AppContext();
        var todoEntities = await appContext.Todos.ToListAsync();

        return todoEntities
            .Select(te => new Todo { Name = te.Name, Status = (Status)te.Status })
            .ToList();
    }

    public async Task<Todo> AddTodo(string name)
    {
        using var appContext = new AppContext();
        var todoEntity = new TodoEntity() { Name = name };
        appContext.Todos.Add(todoEntity);
        await appContext.SaveChangesAsync();

        return new Todo { Name = todoEntity.Name };
    }
}

public interface ITodoDataStore
{
    public Task<List<Todo>> GetTodos();
    public Task<Todo> AddTodo(string name);
}

public class AppContext : DbContext
{
    public DbSet<TodoEntity> Todos => Set<TodoEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            "Server=localhost,57000;Database=App;User Id=sa;Password=pa33word!;TrustServerCertificate=True;"
        );
    }
}

[Table("Todos")]
public class TodoEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Status { get; set; }
}
