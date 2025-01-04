var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Rewind_ApiService>("apiservice");

builder.AddProject<Projects.Rewind_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
