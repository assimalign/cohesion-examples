using System;

using Assimalign.Cohesion.Database;
using Assimalign.Cohesion.Database.Hosting;
using Assimalign.Cohesion.Database.Sql;
using Assimalign.Cohesion.Hosting;
using Acme.Database;

DatabaseApplicationBuilder builder = DatabaseApplication.CreateBuilder(args);

await using SqlDatabaseEngine engine = builder.AddSqlDatabase(options =>
{
    options.EngineName = "acme-sql";
    options.RootPath = Resource.Mounts.Data.Path
        ?? throw new InvalidOperationException("The database data mount must have a materialized path.");
});

builder.AddDatabase(engine, "customers", database =>
{
    database.Table<Customer>("Customers", table =>
    {
        table.Key(customer => customer.Id);
        table.Index(customer => customer.Email);
    });
    // Principals and grants are declared here once the SQL engine can migrate them (Database MVP: runtime
    // principal mutation). Until then the db endpoint accepts the AllowAll authenticator that ships today.
});

builder.AddSqlServer(engine, options => options.Listen(Resource.Endpoints.Db));

await using DatabaseApplication application = builder.Build();
await application.RunAsync();

internal sealed record Customer(long Id, string Name, string Email);
