var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Apex_RuleGrid>("apex-rule-grid");

builder.Build().Run();
