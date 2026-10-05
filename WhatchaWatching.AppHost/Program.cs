var builder = DistributedApplication.CreateBuilder(args);

var mysql = builder.AddMySql("mysql")
    .WithDataVolume();

// The resource name must match DatabaseConstants.CONNECTION_NAME: it becomes ConnectionStrings:DefaultConnection in the web app.
var database = mysql.AddDatabase("DefaultConnection", "whatchawatching");

builder.AddProject<Projects.WhatchaWatching>("web")
    .WithExternalHttpEndpoints()
    .WithReference(database)
    .WaitFor(database);

builder.Build().Run();
