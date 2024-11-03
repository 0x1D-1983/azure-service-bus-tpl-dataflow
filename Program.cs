using TplAzureServiceBusWorker;
using TplAzureServiceBusWorker.Config;
using TplAzureServiceBusWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

var config = builder.Configuration;

builder.Services.AddOptions();
builder.Services.Configure<AzureServiceBusConfig>(config.GetSection(nameof(AzureServiceBusConfig)));

builder.Services.AddHostedService<StreamProcessor>();

var host = builder.Build();
host.Run();
