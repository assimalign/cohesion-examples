using Assimalign.Cohesion.Database;
using Assimalign.Cohesion.Database.Hosting;
using Assimalign.Cohesion.Database.Sql;
using Assimalign.Cohesion.Database.Storage;
using Example.AppC.Database;

// The appc-database resource, code-first: tables, indexes, constraints, a custom type, a function and a trigger are
// all C#; schema compile and migrations operate on this model. The Database builder API used here is illustrative — the Database area owns it. What this scaffold fixes is only that the resource is an ordinary executable (Program.cs), that orchestration is the opt-in in the csproj, and that its orchestration-facing facts live there.
DatabaseApplicationBuilder builder = DatabaseApplication.CreateBuilder(args);

SqlDatabaseEngine engine = builder.AddSqlDatabase(options =>
{
    options.RootPath   = Resource.Mounts.Data;
    options.Durability = Resource.Settings.DatabaseDurability;
});

builder.AddDatabase(engine, "inventory", database =>
{
    database.Type<Money>(type => type.Decimal(precision: 18, scale: 2));                          // custom type

    database.Table<Item>(table =>
    {
        table.Key(x => x.Sku);
        table.Check(x => x.OnHand >= x.Reserved);
    });
    database.Table<Movement>(table =>
    {
        table.Key(x => x.Id);
        table.References<Item>(x => x.Sku);
    });

    database.Function("available", (string sku) => Sql.Scalar<Item>(i => i.OnHand - i.Reserved, i => i.Sku == sku));                                 // SQL-callable, defined in C#
    database.Trigger<Item>(TriggerEvent.AfterInsert, (transaction, row) => transaction.Audit("item.created", row.Sku));

    database.Principal("appc-api", principal => principal.Grant(Permission.ReadWrite, "Items", "Movements"));   // identity is database-scoped
});

builder.AddSqlServer(engine, server => server.Listen(Resource.Endpoints.Db));
// The `admin` endpoint (health, readiness, commands) is the Database default control plane, wired by the opt-in.

await builder.Build().RunAsync();   // provisioning runs before the server accepts (Database.Hosting starts services before servers)

namespace Example.AppC.Database
{
    public sealed record Item(string Sku, string Name, int OnHand, int Reserved, DateTime UpdatedAt);
    public sealed record Movement(long Id, string Sku, int Delta, string Reason, DateTime MovedAt);
    public readonly record struct Money(decimal Amount);
}
