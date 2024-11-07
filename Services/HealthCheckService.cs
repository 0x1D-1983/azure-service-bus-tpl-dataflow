using System.Net;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TplAzureServiceBusWorker.Models;

namespace TplAzureServiceBusWorker.Services
{
    public class HealthCheckService : BackgroundService
    {
        private readonly ILogger<HealthCheckService> _logger;
        private readonly HttpListener _httpListener;

        public HealthCheckService(
            ILogger<HealthCheckService> logger
            )
        {
            _httpListener = new HttpListener();
            _logger = logger;
        }

        protected async override Task<HealthCheckResult> ExecuteAsync(CancellationToken stoppingToken)
        {
            _httpListener.Prefixes.Add($"http://*:80/healthz/live/");
            _httpListener.Prefixes.Add($"http://*:80/healthz/ready/");

            _httpListener.Start();
            _logger.LogInformation($"Healthcheck listening...");

            while (!stoppingToken.IsCancellationRequested)
            {
                HttpListenerContext ctx = null;
                try
                {
                    ctx = await _httpListener.GetContextAsync();
                }
                catch (HttpListenerException ex)
                {
                    if (ex.ErrorCode == 995) return await Task.FromResult(HealthCheckResult.Degraded());
                }

                if (ctx == null) continue;

                var response = ctx.Response;
                response.ContentType = "text/json";
                response.Headers.Add(HttpResponseHeader.CacheControl, "no-store, no-cache");
                response.StatusCode = (int)HttpStatusCode.OK;

                var messageBytes = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(HealthCheckResult.Healthy()));
                response.ContentLength64 = messageBytes.Length;
                await response.OutputStream.WriteAsync(messageBytes, 0, messageBytes.Length);
                response.OutputStream.Close();
                response.Close();
            }

            return await Task.FromResult(HealthCheckResult.Healthy());
        }
    }
}

