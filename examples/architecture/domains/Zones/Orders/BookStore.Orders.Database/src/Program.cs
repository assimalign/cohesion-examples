// orders-db: the Orders application's database. A plain Database program: the schema is application code. Its disk,
// its backups and its lock are spec in the orders AppHost's app.Database.Add<Projects.X>("orders-db", ...), and the
// environment says where it runs. See users-db for how a schema change rolls out, and for the SQL port that this
// program does not open as written.

using Assimalign.Cohesion.Database.Hosting;
using Assimalign.Cohesion.Database.Sql;

using BookStore.Orders;

DatabaseApplicationBuilder builder = DatabaseApplication.CreateBuilder(args);

builder.AddSql("orders", engine =>
{
    engine.AddDatabase("orders", database =>
    {
        database.Schema(schema =>
        {
            schema.Table<Order>("Orders", table =>
            {
                table.Column(order => order.Id);
                table.Column(order => order.CustomerId);
                table.Column(order => order.Total);
                table.Column(order => order.Status);
                table.Column(order => order.PlacedAt);
                table.PrimaryKey(order => order.Id);
            });
        });
    });
});

await using DatabaseApplication application = builder.Build();
await application.RunAsync();
