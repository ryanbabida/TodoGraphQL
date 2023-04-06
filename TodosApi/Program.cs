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
    public User User { get; set; }
}

public class User
{
    public string Name { get; set; }
}

public class Query
{
    public List<Todo> GetTodos()
    {
        var todos = new MockTodoDataStore();
        return todos.GetTodos();
    }
}

public class Mutation
{
    public Todo AddTodo(string name)
    {
        var todos = new MockTodoDataStore();
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

public interface ITodoDataStore
{
    public List<Todo> GetTodos();
    public Todo AddTodo(string name);
}
