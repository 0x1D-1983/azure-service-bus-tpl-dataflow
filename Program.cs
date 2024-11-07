using Microsoft.AspNetCore.Server.Kestrel.Core;
using Prometheus;
using TplAzureServiceBusWorker.Config;
using TplAzureServiceBusWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

var config = builder.Configuration;

builder.Services.AddOptions();
builder.Services.Configure<AzureServiceBusConfig>(config.GetSection(nameof(AzureServiceBusConfig)));
builder.Services.AddHealthChecks()
    .AddCheck<HealthCheckService>("TPL Bus health check")
    .ForwardToPrometheus();

builder.Services.AddHostedService<StreamProcessor>();

var metricServer = new KestrelMetricServer(port: 80, url: "/metrics");
metricServer.Start();

var host = builder.Build();
host.Run();
