using Assimalign.Cohesion.Database;
using Assimalign.Cohesion.Database.Hosting;
using Assimalign.Cohesion.Database.Sql;
using Assimalign.Cohesion.Database.Storage;
using Example.AppB.Database;

// The appb-database resource, code-first: tables, indexes, constraints, a custom type, a function and a trigger are
// all C#; schema compile and migrations operate on this model. The Database builder API used here is illustrative — the Database area owns it. What this scaffold fixes is only that the resource is an ordinary executable (Program.cs), that orchestration is the opt-in in the csproj, and that its orchestration-facing facts live there.
DatabaseApplicationBuilder builder = DatabaseApplication.CreateBuilder(args);

SqlDatabaseEngine engine = builder.AddSqlDatabase(options =>
{
    options.RootPath   = Resource.Mounts.Data;
    options.Durability = Resource.Settings.DatabaseDurability;
});

builder.AddDatabase(engine, "billing", database =>
{
    database.Type<Money>(type => type.Decimal(precision: 18, scale: 2));                          // custom type

    database.Table<Invoice>(table =>
    {
        table.Key(x => x.Id);
        table.Index(x => x.AccountId); table.Check(x => x.Amount >= 0);
    });
    database.Table<Payment>(table =>
    {
        table.Key(x => x.Id);
        table.References<Invoice>(x => x.InvoiceId);
    });

    database.Function("invoice_balance", (long invoiceId) => Sql.Scalar<Invoice>(i => i.Amount, i => i.Id == invoiceId) - Sql.Sum<Payment>(p => p.Amount, p => p.InvoiceId == invoiceId));                                 // SQL-callable, defined in C#
    database.Trigger<Invoice>(TriggerEvent.AfterInsert, (transaction, row) => transaction.Audit("invoice.issued", row.Id));

    database.Principal("appb-api", principal => principal.Grant(Permission.ReadWrite, "Invoices", "Payments"));   // identity is database-scoped
});

builder.AddSqlServer(engine, server => server.Listen(Resource.Endpoints.Db));
// The `admin` endpoint (health, readiness, commands) is the Database default control plane, wired by the opt-in.

await builder.Build().RunAsync();   // provisioning runs before the server accepts (Database.Hosting starts services before servers)

namespace Example.AppB.Database
{
    public sealed record Invoice(long Id, long AccountId, DateTime IssuedAt, DateTime DueAt, decimal Amount, bool Paid);
    public sealed record Payment(long Id, long InvoiceId, DateTime ReceivedAt, decimal Amount);
    public readonly record struct Money(decimal Amount);
}
