using Assimalign.Cohesion.Database;
using Assimalign.Cohesion.Database.Sql;
using Assimalign.Cohesion.Database.Hosting;



var builder = new DatabaseApplicationBuilder(new DatabaseApplicationOptions()
{
     
});



var engine = SqlDatabaseEngine.Create(new SqlDatabaseEngineOptions()
{
    EngineName = "",
    StorageStrategy = Sql
});

builder.AddSqlServer(engine, options =>
{
    
});


var database = builder.Build();


await database.RunAsync();