# TodoGraphQL

To run the project:
```
cd TodosApi
dotnet run
```

and navigate to `localhost:5015/graphql`

Query
```
query {
  todos {
    name
  }
}
```

Mutation
```
mutation {
  addTodo(name: "Watch basketball") {
    name
  }
}
```