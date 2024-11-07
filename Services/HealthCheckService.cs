using System;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TplAzureServiceBusWorker.Services
{
    public class HealthCheckService : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            //var result = _rnd.Next(5) == 0
            //? HealthCheckResult.Healthy()
            //: HealthCheckResult.Unhealthy("Failed random");

            return Task.FromResult(HealthCheckResult.Healthy());
        }
    }
}

