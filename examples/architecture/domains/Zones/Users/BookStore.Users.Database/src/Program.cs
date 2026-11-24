// users-db: the Users application's database. A plain Database program: the schema is application code, in this
// project.
//
// Nothing here says where it runs, how big its disk is, when it is backed up, or who may connect. The users AppHost's
// app.Database.Add<Projects.X>("users-db", ...) says the disk size, the backup policy and the lock. The Database
// provider turns that into a stateful workload, a volume and a credential, and the environment says where they run.
//
// As written this program opens no SQL port: on main an engine has a server only where its builder calls AddServer,
// and no host adds one, so users-api could not connect. The owner said on 2026-11-24 that this program's own code
// opens the port. The call a managed program makes, and how the port it binds is mapped, are not designed (v2 §8.3).
//
// A new schema ships as a new image of this program. The provider backs up with the outgoing image, restarts the one
// writer, and holds users-api's rollout until this program reports SchemaCurrent. A destructive change holds (CS03105)
// until it is approved. Who approves it is open: v2 names the Sensitive approve-schema-change action for a hub's
// private database only (rules R6, R7), and no such action for a database that an AppHost declares (v2 §15).
//
// The schema verbs are today's Database builder; they follow whatever the Database builder redesign lands.

using Assimalign.Cohesion.Database.Hosting;
using Assimalign.Cohesion.Database.Sql;

using BookStore.Users;

DatabaseApplicationBuilder builder = DatabaseApplication.CreateBuilder(args);

builder.AddSql("users", engine =>
{
    engine.AddDatabase("users", database =>
    {
        database.Schema(schema =>
        {
            schema.Table<User>("Users", table =>
            {
                table.Column(user => user.Id);
                table.Column(user => user.Email);
                table.Column(user => user.DisplayName);
                table.Column(user => user.CreatedAt);
                table.PrimaryKey(user => user.Id);
            });
        });
    });
});

await using DatabaseApplication application = builder.Build();
await application.RunAsync();
