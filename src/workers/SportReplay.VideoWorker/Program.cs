using Serilog;
using SportReplay.Application;
using SportReplay.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables();
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
builder.Services.AddSerilog();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
var host = builder.Build();
await host.RunAsync();
