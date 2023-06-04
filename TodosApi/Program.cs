using System.ComponentModel.DataAnnotations.Schema;
using HotChocolate.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Trace;

internal sealed class Program
{
    public static List<Todo> _Todos =
        new()
        {
            new Todo { Name = "Make breakfast" },
            new Todo { Name = "Study GraphQL" }
        };

    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddScoped<ITodoDataStore, TodoDataStore>()
            .AddScoped<ITodoService, TodoService>()
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddInstrumentation(options =>
                {
                    options.RenameRootActivity = true;
                    options.IncludeDocument = true;
                    options.Scopes = ActivityScopes.All;
                    options.IncludeDataLoaderKeys = true;
                    options.RequestDetails = RequestDetails.All;
                }
            );

        builder.Services
            .AddOpenTelemetry()
            .WithTracing(
                builder =>
                    builder
                        .AddSource("Todos")
                        .AddAspNetCoreInstrumentation(options =>
                            {
                                options.RecordException = true;
                            })
                        .AddHttpClientInstrumentation()
                        .AddSqlClientInstrumentation(options =>
                            {
                                options.SetDbStatementForText = true;
                                options.SetDbStatementForStoredProcedure = true;
                                options.RecordException = true;
                            })
                        .AddOtlpExporter(options =>
                            options.Endpoint = new Uri("http://localhost:4317"))
                        .AddConsoleExporter()
            );

        var app = builder.Build();

        app.MapGraphQL();

        app.MapGet("/todos", async (ITodoService todoService) => await todoService.GetTodosAsync());

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
    Unknown,
    Incomplete,
    InProgress,
    Done
}

public class Query
{
    public List<Todo> GetTodos([Service] ITodoDataStore dataStore) =>
        dataStore.GetTodosAsync().Result;
}

public class Mutation
{
    public Todo AddTodo([Service] ITodoDataStore dataStore, string name) =>
        dataStore.AddTodoAsync(name).Result;
}

public class TodoService : ITodoService
{
    private ITodoDataStore TodoDataStore { get; set; }

    public TodoService(ITodoDataStore todoDataStore) => this.TodoDataStore = todoDataStore;

    public async Task<IResult> GetTodosAsync()
    {
        var todos = await this.TodoDataStore.GetTodosAsync();
        return Results.Ok(todos);
    }

    public async Task<IResult> AddTodoAsync(string name)
    {
        var todo = await this.TodoDataStore.AddTodoAsync(name);
        return Results.Created("", todo);
    }
}

public interface ITodoService
{
    public Task<IResult> GetTodosAsync();
    public Task<IResult> AddTodoAsync(string name);
}

public class MockTodoDataStore : ITodoDataStore
{
    public Task<List<Todo>> GetTodosAsync() => Task.FromResult(Program._Todos);

    public Task<Todo> AddTodoAsync(string name)
    {
        var todo = new Todo { Name = name };
        Program._Todos.Add(todo);
        return Task.FromResult(todo);
    }
}

public class TodoDataStore : ITodoDataStore
{
    public async Task<List<Todo>> GetTodosAsync()
    {
        using var appContext = new AppContext();
        var todoEntities = await appContext.Todos.ToListAsync();

        return todoEntities
            .Select(te => new Todo { Name = te.Name, Status = (Status)te.Status })
            .ToList();
    }

    public async Task<Todo> AddTodoAsync(string name)
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
    public Task<List<Todo>> GetTodosAsync();
    public Task<Todo> AddTodoAsync(string name);
}

public class AppContext : DbContext
{
    public DbSet<TodoEntity> Todos => this.Set<TodoEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlServer(
            "Server=localhost,57000;Database=App;User Id=sa;Password=pa33word!;TrustServerCertificate=True;"
        );
}

[Table("Todos")]
public class TodoEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Status { get; set; }
}
