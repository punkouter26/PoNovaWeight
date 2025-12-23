var builder = DistributedApplication.CreateBuilder(args);

// Azure Storage with Azurite emulator container
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator => emulator
        .WithLifetime(ContainerLifetime.Persistent));

var tableStorage = storage.AddTables("tables");

// API service with storage references
var api = builder.AddProject<Projects.PoNovaWeight_Api>("api")
    .WithReference(tableStorage)
    .WaitFor(storage)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.Build().Run();
