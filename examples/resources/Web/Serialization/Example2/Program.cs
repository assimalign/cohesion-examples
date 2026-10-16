// Web.Serialization, Example 2: content negotiation with your own format.
//
// The content-serialization registry holds a reader or writer per media type. This application
// registers JSON with snake_case names and its own CSV writer for product lists. A handler that
// returns a value is answered in the format the client's Accept header asks for, and with 406 Not
// Acceptable when no registered writer can produce it.
//
//   dotnet run
//   curl http://localhost:5000/products                          (JSON: the first registered writer)
//   curl -H "Accept: text/csv" http://localhost:5000/products    (CSV)
//   curl -i -H "Accept: text/html" http://localhost:5000/products (406)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;
using Assimalign.Cohesion.Web.Serialization;

Product[] products =
[
    new(1, "Pen", 2.50m),
    new(2, "Notebook", 4.25m),
    new(3, "Stapler", 12.00m),
];

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddRouting()
    .AddContentSerialization(serialization => serialization
        .AddJson(AppJsonContext.Default, json => json.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower)
        .AddWriter(new ProductCsvWriter()));

await using WebApplication application = builder.Build();

application.UseRouting();

application.MapGet("/products", () => products);

await application.RunAsync();

record Product(int Id, string Name, decimal UnitPrice);

[JsonSerializable(typeof(Product[]))]
partial class AppJsonContext : JsonSerializerContext;

// Writes product lists as CSV.
sealed class ProductCsvWriter : IContentWriter
{
    public IReadOnlyList<HttpMediaType> MediaTypes { get; } = [HttpMediaType.Parse("text/csv")];

    public bool CanWrite(Type type) => type == typeof(Product[]);

    public async Task WriteAsync(IHttpResponse response, object? value, Type type, HttpMediaType contentType, CancellationToken cancellationToken = default)
    {
        StringBuilder csv = new("id,name,unit_price\n");
        foreach (Product product in (Product[])value!)
        {
            csv.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{product.Id},{product.Name},{product.UnitPrice}"));
        }

        response.Headers[HttpHeaderKey.ContentType] = "text/csv; charset=utf-8";
        await response.Body.WriteAsync(Encoding.UTF8.GetBytes(csv.ToString()), cancellationToken);
    }
}
