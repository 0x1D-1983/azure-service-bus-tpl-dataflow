using System.Threading.Tasks.Dataflow;
using Azure.Messaging.ServiceBus;

namespace TplAzureServiceBusWorker.Tpl
{
	public class ServiceBusSourceBlock : ISourceBlock<ServiceBusReceivedMessage>
    {
        private readonly ILogger<ServiceBusSourceBlock> _logger;
        private readonly BufferBlock<ServiceBusReceivedMessage> _messageBuffer;
        private readonly ServiceBusReceiver _receiver;
        private readonly CancellationToken _cancellationToken;
        private readonly int _consumeReportInterval;
        private bool _wasBlocked;
        private const int RetryBackoff = 2000;
        private long _recordsConsumed;

        public ServiceBusSourceBlock(
            ILogger<ServiceBusSourceBlock> logger,
            ServiceBusReceiver receiver,
            int consumeReportInterval,
            CancellationToken cancellationToken)
		{
            _logger = logger;
            _cancellationToken = cancellationToken;
            _receiver = receiver;
            _messageBuffer = new BufferBlock<ServiceBusReceivedMessage>();
            _consumeReportInterval = consumeReportInterval;
        }

        public Task StartAsync()
        {
            return Task.Factory.StartNew(async () =>
            {
                _logger.LogInformation("Subscribed to topic");
                while (!_cancellationToken.IsCancellationRequested)
                {
                    ServiceBusReceivedMessage? message = default;

                    try
                    {
                        message = await _receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5));
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError($"Error consuming {exception}");
                        _logger.LogInformation("Cancelling now");
                        throw;
                    }
                    if (message == null) continue;

                    // Note that if the time spent in this section 
                    // exceeds the max.poll.timeout this consumer 
                    // will get removed from the group and a rebalance will occur
                    while (!await _messageBuffer.SendAsync(message))
                    {
                        _logger.LogWarning("message buffer full, blocking until available");
                        _wasBlocked = true;
                        await Task.Delay(RetryBackoff);
                    }

                    if (_wasBlocked)
                    {
                        _logger.LogInformation("message buffer accepting records again");
                        _wasBlocked = false;
                    }

                    if (++_recordsConsumed % _consumeReportInterval == 0)
                    {
                        _logger.LogInformation($"{_recordsConsumed} records consumed so far");
                    }
                }
                _logger.LogInformation("Dropping out of consume loop");
            });
        }

        public Task Completion
        {
            get
            {
                _logger.LogInformation("Complete on the SourceBlock called");
                return _messageBuffer.Completion;
            }
        }

        public void Complete()
        {
            _logger.LogInformation("Complete on the SourceBlock called");
            _messageBuffer.Complete();
        }

        public ServiceBusReceivedMessage? ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<ServiceBusReceivedMessage> target, out bool messageConsumed) =>
            ((ISourceBlock<ServiceBusReceivedMessage>) _messageBuffer).ConsumeMessage(messageHeader, target,
            out messageConsumed);

        public void Fault(Exception exception)
        {
            ((ISourceBlock<ServiceBusReceivedMessage>)_messageBuffer).Fault(exception);
        }

        public IDisposable LinkTo(ITargetBlock<ServiceBusReceivedMessage> target, DataflowLinkOptions linkOptions) =>
            _messageBuffer.LinkTo(target, linkOptions);

        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<ServiceBusReceivedMessage> target) =>
            ((ISourceBlock<ServiceBusReceivedMessage>)_messageBuffer).ReleaseReservation(messageHeader, target);

        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<ServiceBusReceivedMessage> target) =>
            ((ISourceBlock<ServiceBusReceivedMessage>)_messageBuffer).ReserveMessage(messageHeader, target);
    }
}

