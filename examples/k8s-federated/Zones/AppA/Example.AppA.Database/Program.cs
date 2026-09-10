using Assimalign.Cohesion.Database;
using Assimalign.Cohesion.Database.Hosting;
using Assimalign.Cohesion.Database.Sql;
using Assimalign.Cohesion.Database.Storage;
using Example.AppA.Database;

// The appa-database resource, code-first: tables, indexes, constraints, a custom type, a function and a trigger are
// all C#; schema compile and migrations operate on this model. The Database builder API used here is illustrative — the Database area owns it. What this scaffold fixes is only that the resource is an ordinary executable (Program.cs), that orchestration is the opt-in in the csproj, and that its orchestration-facing facts live there.
DatabaseApplicationBuilder builder = DatabaseApplication.CreateBuilder(args);

SqlDatabaseEngine engine = builder.AddSqlDatabase(options =>
{
    options.RootPath   = Resource.Mounts.Data;
    options.Durability = Resource.Settings.DatabaseDurability;
});

builder.AddDatabase(engine, "orders", database =>
{
    database.Type<Money>(type => type.Decimal(precision: 18, scale: 2));                          // custom type

    database.Table<Order>(table =>
    {
        table.Key(x => x.Id);
        table.Index(x => x.CustomerId);
    });
    database.Table<OrderLine>(table =>
    {
        table.Key(x => x.Id);
        table.References<Order>(x => x.OrderId);
    });

    database.Function("order_total", (long orderId) => Sql.Sum<OrderLine>(l => l.Quantity * l.UnitPrice, l => l.OrderId == orderId));                                 // SQL-callable, defined in C#
    database.Trigger<Order>(TriggerEvent.AfterInsert, (transaction, row) => transaction.Audit("order.placed", row.Id));

    database.Principal("appa-api", principal => principal.Grant(Permission.ReadWrite, "Orders", "OrderLines"));   // identity is database-scoped
});

builder.AddSqlServer(engine, server => server.Listen(Resource.Endpoints.Db));
// The `admin` endpoint (health, readiness, commands) is the Database default control plane, wired by the opt-in.

await builder.Build().RunAsync();   // provisioning runs before the server accepts (Database.Hosting starts services before servers)

namespace Example.AppA.Database
{
    public sealed record Order(long Id, long CustomerId, DateTime PlacedAt, string Status, decimal Total);
    public sealed record OrderLine(long Id, long OrderId, string Sku, int Quantity, decimal UnitPrice);
    public readonly record struct Money(decimal Amount);
}
