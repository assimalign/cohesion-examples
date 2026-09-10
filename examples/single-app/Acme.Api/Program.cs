using Acme.Api;
using Assimalign.Cohesion.Database.Sql.Client;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Api;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

ISqlClient customers = SqlClient.Create(new SqlClientOptions
{
    Settings = DatabaseConnectionSettings.For(Resource.References.AcmeDatabase.Db.Endpoint, database: "customers", principal: Resource.Name),
    ConnectionFactory = Resource.References.AcmeDatabase.Db.ConnectionFactory(),   // loopback in-process, TCP elsewhere
});
builder.Services.AddSingleton(customers);

WebApplication app = builder.Build();
app.UseRouting();
app.MapGet("/customers", () => customers.Query<Customer>("SELECT * FROM Customers ORDER BY Name LIMIT @take", new { take = Resource.Settings.CustomersPageSize }));
app.MapGet("/customers/{id}", (long id) => customers.QuerySingle<Customer>("SELECT * FROM Customers WHERE Id = @id", new { id }));
app.MapPost("/customers", (Customer customer) => customers.Execute("INSERT INTO Customers (Name, Email) VALUES (@Name, @Email)", customer));

await app.RunAsync();

namespace Acme.Api
{
    public sealed record Customer(long Id, string Name, string Email);
}
