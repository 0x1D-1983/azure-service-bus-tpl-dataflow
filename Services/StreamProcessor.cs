using System.Threading.Tasks.Dataflow;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using TplAzureServiceBusWorker.Config;
using TplAzureServiceBusWorker.Models;
using TplAzureServiceBusWorker.Tpl;

namespace TplAzureServiceBusWorker.Services
{
    public class StreamProcessor : IHostedService, IAsyncDisposable
    {
        private readonly ILogger<StreamProcessor> _logger;
        private readonly IServiceProvider _services;
        private readonly ServiceBusClient _client;
        private readonly ServiceBusReceiver _receiver;
        private readonly AzureServiceBusConfig _config;

        private readonly ILogger<ServiceBusSourceBlock> _sourceBlockLogger;
        private readonly ILogger<ServiceBusSinkBlock> _sinkBlockLogger;

        ServiceBusSourceBlock? _sourceBlock;
        ServiceBusSinkBlock? _sinkBlock;

        public StreamProcessor(
            ILogger<StreamProcessor> logger,
            IServiceProvider services,
            IOptionsMonitor<AzureServiceBusConfig> config)
		{
            try
            {
                _logger = logger;
                _services = services;
                _config = config.CurrentValue;
                _client = new ServiceBusClient(_config.ConnectionString);
                _receiver = _client.CreateReceiver(_config.Topic, _config.Subscription);

                _sourceBlockLogger = _services.GetService<ILogger<ServiceBusSourceBlock>>() ?? throw new ArgumentNullException(nameof(ILogger<ServiceBusSourceBlock>));
                _sinkBlockLogger = _services.GetService<ILogger<ServiceBusSinkBlock>>() ?? throw new ArgumentNullException(nameof(ILogger<ServiceBusSinkBlock>));
            }
            catch (Exception)
            {
                ServiceHealth.IsHealthy = false;
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _receiver.DisposeAsync();
            await _client.DisposeAsync();
            await ValueTask.CompletedTask;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _sourceBlock = new(_sourceBlockLogger, _receiver, _config.ReportInterval, cancellationToken);
                _logger.LogInformation("Starting the source block");
                await _sourceBlock.StartAsync();

                _sinkBlock = new(_sinkBlockLogger, _receiver, cancellationToken);
                _logger.LogInformation("Starting the sink block");
                await _sinkBlock.StartAsync();


                var linkOptions = new DataflowLinkOptions
                {
                    PropagateCompletion = true
                };

                var standardBlockOptions = new ExecutionDataflowBlockOptions()
                {
                    CancellationToken = cancellationToken,
                    BoundedCapacity = 10_000
                };

                var desFunc = DeserializeBodyHelper.MapPurchase();

                var deserializeBlock =
                    new TransformBlock<ServiceBusReceivedMessage, Tuple<ServiceBusReceivedMessage, Item?>>(desFunc, standardBlockOptions);

                _sourceBlock.LinkTo(deserializeBlock, linkOptions);
                deserializeBlock.LinkTo(_sinkBlock, linkOptions);

                await Task.CompletedTask;
            }
            catch(Exception ex)
            {
                ServiceHealth.IsHealthy = false;
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stop requested on the stream processor");

            //if (_sinkBlock != null) _sinkBlock.Complete(); ;
            if (_sourceBlock != null) _sourceBlock.Complete();

            await _receiver.CloseAsync(cancellationToken);
            await _client.DisposeAsync();
            await Task.CompletedTask;
        }
    }
}

