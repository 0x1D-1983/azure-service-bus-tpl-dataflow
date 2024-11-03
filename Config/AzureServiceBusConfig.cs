using System;
namespace TplAzureServiceBusWorker.Config
{
	public class AzureServiceBusConfig
	{
		public string? ConnectionString { get; set; }
		public string? Topic { get; set; }
		public string? Subscription { get; set; }
		public int ReportInterval { get; set; }

	}
}

