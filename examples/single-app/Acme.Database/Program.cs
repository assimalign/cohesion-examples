using Acme.Database;
using Assimalign.Cohesion.Database;
using Assimalign.Cohesion.Database.Hosting;
using Assimalign.Cohesion.Database.Sql;

// The acme-database resource: a code-first SQL database (the Database builder API is illustrative; the Database area owns it).
DatabaseApplicationBuilder builder = DatabaseApplication.CreateBuilder(args);

SqlDatabaseEngine engine = builder.AddSqlDatabase(options => options.RootPath = Resource.Mounts.Data);

builder.AddDatabase(engine, "customers", database =>
{
    database.Table<Customer>(table =>
    {
        table.Key(x => x.Id);
        table.Unique(x => x.Email);
    });
    database.Principal("acme-api", principal => principal.Grant(Permission.ReadWrite, "Customers"));
});

builder.AddSqlServer(engine, server => server.Listen(Resource.Endpoints.Db));

await builder.Build().RunAsync();

namespace Acme.Database
{
    public sealed record Customer(long Id, string Name, string Email);
}
