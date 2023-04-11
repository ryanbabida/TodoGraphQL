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
            .AddDbContext<AppContext>()
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>();

        var app = builder.Build();

        app.MapGraphQL();

        app.Run();
    }
}

public class Todo
{
    public string Name { get; set; }
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
        return todos.GetTodos();
    }
}

public class Mutation
{
    public Todo AddTodo(string name)
    {
        var todos = new TodoDataStore();
        return todos.AddTodo(name);
    }
}

public class MockTodoDataStore : ITodoDataStore
{
    public List<Todo> GetTodos() => Program._Todos;

    public Todo AddTodo(string name)
    {
        var todo = new Todo { Name = name };
        Program._Todos.Add(todo);
        return todo;
    }
}

public class TodoDataStore : ITodoDataStore
{
    public List<Todo> GetTodos()
    {
        using var appContext = new AppContext();
        var todoEntities = appContext.Todos.ToList();

        return todoEntities.Select(te =>
            new Todo
            {
                Name = te.Name,
                Status = (Status)te.Status
            })
        .ToList();
    }

    public Todo AddTodo(string name)
    {
        using var appContext = new AppContext();
        var todoEntity = new TodoEntity() { Name = name };
        appContext.Todos.Add(todoEntity);
        appContext.SaveChanges();

        return new Todo { Name = todoEntity.Name };
    }
}

public interface ITodoDataStore
{
    public List<Todo> GetTodos();
    public Todo AddTodo(string name);
}

public class AppContext : DbContext
{
    public DbSet<TodoEntity> Todos => Set<TodoEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=localhost,57000;Database=App;User Id=sa;Password=pa33word!;TrustServerCertificate=True;");
    }
}

[Table("Todos")]
public class TodoEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; }
    public int Status { get; set; }
}
