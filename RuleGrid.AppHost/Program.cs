var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.RuleGrid>("rule-grid");

builder.Build().Run();
