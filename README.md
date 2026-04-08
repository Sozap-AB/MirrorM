MirrorM is an ORM for .NET projects. Currently works with PostgreSQL databases only.

# Key features #

  * In-memory context: what is in the memory context is not just cache, it's the extension of the database. Which means the following:
	* Models cannot be duplicated in the same context. Even manually you cannot create a model representing the same database row. Same object is gonna be returned by every query, relation and even raw-SQL query, which means no more outdated objects, changes overwrites and other inconsistencies.
	* Queries will respect in-memory changes in general. There are some reasonable exceptions to this rule (explained here), but in the majority of cases it works just as you expect it to work.
	* Changes uploaded to the database in batches (usually one batch at the end of work to save all changes at once), which reduces the number of database calls.
  * LINQ+SQL oriented design: with MirrorM it's easy to combine LINQ queries with raw SQL queries, which gives you the best of both worlds. You can write complex queries in SQL and still get all the benefits of in-memory context, and write other queries using convenient LINQ-like API.
  * Simple generated SQL: MirrorM generates readable SQL queries, which you might expect writing yourself, doing the same job.
  * Flexible SQL generation: If you still don't like generated SQL, or want to use another database client library you can write your own adapter project, or rewrite existing one, working with a database client is completely localized in a separate project.

# Pre Requirements #

Can be installed into .NET 6.0+ projects. Currently works with PostgreSQL databases only.

# Getting started #

1. Install NuGet package:
    ```sh
    Install-Package MirrorM
    ```
2. Create database and tables:
    ```sql
    CREATE DATABASE my_database;

    -- Connect to my_database

    CREATE TABLE users (
        id UUID PRIMARY KEY,
        name TEXT NOT NULL,
        _version BIGINT NOT NULL,
        _updated_at TIMESTAMP NOT NULL,
        _created_at TIMESTAMP NOT NULL
    );
    ```
3. Create model classes:
	```csharp
    namespace App.Models
    {
        [Entity("users")]
        public class User : Entity
        {
            public const string FIELD_NAME = "name";

            [Field(FIELD_NAME)]
            public string Name
            {
                get => GetValue<string>(FIELD_NAME);
                set => SetValue(FIELD_NAME, value);
            }
            
            // constructor for creating new user
            public User(IContext db, string name) : base(db)
            {
                Name = name;
            }

            // constructor for loading from database
            public User(IContext db, IFields fields) : base(db, fields)
            {
            }
        }
    }
    ```
4. Create context and work with models:
    ```csharp
    var contextProvider = new ContextProviderConfigBuilder()
        .UseNpgsqlAdapter("Host=localhost;Port=5432;Database=my_database;Username=my_user;Password=my_password")
        .Build();

    // Create and save new user
    
    using (var context = await contextProvider.CreateContextAsync())
    {
        var newUser = new User(context, "Tom");

        await context.CommitAsync();
    }
    
    // Query user

    using (var context = await contextProvider.CreateContextAsync())
    {
        var user = await context.Query<User>().FirstAsync(x => x.Name == "Tom");

        Console.WriteLine($"User found: {user.Name} with ID: {user.Id}");

        await context.CommitAsync();
    }
    ```

And that's it! Now you're using MirrorM in your project. You can find more complex examples in the tests project, see links below.

# Examples #

* [Models](MirrorM.Tests/Models) - shows how to define models.
* [Relations](MirrorM.Tests/DatabaseTests.Relations.cs) - shows how to work with entity relations OneToOne, OneToMany and ManyToMany.
* [Transactions](MirrorM.Tests/DatabaseTests.Transactions.cs) - shows how to work with transactions.
* [Query](MirrorM.Tests/DatabaseTests.Query.cs) - shows how to make various queries.
