using Apex.RuleGrid.Attributes;
using Apex.RuleGrid.ServiceDefaults;
using Apex.RuleGrid.Services;
using Swashbuckle.AspNetCore.Filters;
using Serilog;

// Set up early bootstrap logger
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting Apex.RuleGrid application");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();

    // Add services to the container.
    builder.Services.AddScoped<MongoDbService>();
    builder.Services.AddScoped<RuleEngineService>();

    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<StandardApiResponseActionFilterAttribute>();
    });
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.EnableAnnotations();
        options.ExampleFilters();
    });
    builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();
    
    var app = builder.Build();

    app.MapDefaultEndpoints();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseAuthorization();

    app.MapControllers();

    Log.Information("Apex.RuleGrid application configured and ready to start");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("Shutting down Apex.RuleGrid application");
    Log.CloseAndFlush();
}
