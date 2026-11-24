// identity-db: the database BookStore's IdentityHub keeps its data in. A plain Database program. The hub's database
// package declares the whole database, its schema included, so no schema is written here: the one line below is the
// whole declaration. It passes no database name: the owner decided on 2026-11-23 that the hub's package supplies the
// name the hub connects to (v2 §1). So this program does not show that name.
//
// As written this program opens no SQL port. On main an engine has a server only where its builder calls AddServer,
// and no host adds one, so the hub could not connect. The owner said on 2026-11-24 that this program's own code
// opens the port. The call a managed program makes, and how the port it binds is mapped, are not designed (v2 §8.3).
//
// Its disk, size, backups, lock and administrators are spec in the identity AppHost's
// app.Database.Add<Projects.X>("identity-db", ...). Durability and buffer pool have no member there. This program
// would set them and does not, so identity-db runs with the engine's defaults. On main those are a flush to disk for
// every commit, on a disk that supports one, and a buffer pool of 32 MiB. The hub's Store block had GroupCommit and
// 256Mi. No commit is less safe, because in both modes the engine confirms a commit only once it is on disk. What
// changes is speed.
//
// The two lines that would set them are options of the SQL engine on main. They go inside the callback of AddSql,
// which becomes a block, and the first needs using Assimalign.Cohesion.Database.Storage. This example leaves them
// out to keep the program to its one line:
//
//     engine.Options.Durability = StorageCommitDurability.Grouped;
//     engine.Options.BufferPoolCapacity = 256L * 1024 * 1024;
//
// [proposed] in v2 (§8.3 and §8.4; §15, decision 46): the package's name, which is advisory, its namespace in the
// third using below, the extension's name and the engine's name.

using Assimalign.Cohesion.Database.Hosting;
using Assimalign.Cohesion.Database.Sql;
using Assimalign.Cohesion.IdentityHub.Sql.Schema;

DatabaseApplicationBuilder builder = DatabaseApplication.CreateBuilder(args);

builder.AddSql("main", engine => engine.AddIdentityHubDatabase());

await using DatabaseApplication application = builder.Build();
await application.RunAsync();
