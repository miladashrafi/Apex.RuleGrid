var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.RuleEngine>("ruleengine");

builder.Build().Run();
