using System.Threading.Tasks.Dataflow;
using Azure.Messaging.ServiceBus;
using TplAzureServiceBusWorker.Models;

namespace TplAzureServiceBusWorker.Tpl
{
    public class ServiceBusSinkBlock : ITargetBlock<Tuple<ServiceBusReceivedMessage, Item?>>
    {
        private readonly ILogger<ServiceBusSinkBlock> _logger;
        private readonly BufferBlock<Tuple<ServiceBusReceivedMessage, Item?>> _messageBuffer;
        private readonly ServiceBusReceiver _receiver;
        private readonly CancellationToken _cancellationToken;

        public ServiceBusSinkBlock(
            ILogger<ServiceBusSinkBlock> logger,
            ServiceBusReceiver receiver,
            CancellationToken cancellationToken)
        {
            _logger = logger;
            _cancellationToken = cancellationToken;
            var sinkBlockOptions = new ExecutionDataflowBlockOptions() { BoundedCapacity = 10000 };
            _messageBuffer = new BufferBlock<Tuple<ServiceBusReceivedMessage, Item?>>(sinkBlockOptions);
            _receiver = receiver;
        }

        public Task StartAsync()
        {
            return Task.Factory.StartNew(async () =>
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    var message = await _messageBuffer.ReceiveAsync(_cancellationToken);
                    var messageProps = message.Item1.ApplicationProperties;
                    string storeId = (string)messageProps["StoreId"];

                    _logger.LogInformation(message.Item2?.PrintMessage(storeId));

                    await _receiver.CompleteMessageAsync(message.Item1, _cancellationToken);
                }

                _logger.LogInformation("Dropping out of the produce loop");
            });
        }

        public Task Completion
        {
            get
            {
                _logger.LogInformation("Complete on the SinkBlock called");
                //_producer.Flush();
                //_producer.Dispose();
                return _messageBuffer.Completion;
            }
        }

        public void Complete()
        {
            _logger.LogInformation("Complete on the SinkBlock called");
            _messageBuffer.Complete();
        }

        public void Fault(Exception exception)
        {
            ((IDataflowBlock)_messageBuffer).Fault(exception);
        }

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, Tuple<ServiceBusReceivedMessage, Item?> messageValue, ISourceBlock<Tuple<ServiceBusReceivedMessage, Item?>>? source, bool consumeToAccept) =>
            ((ITargetBlock<Tuple<ServiceBusReceivedMessage, Item?>>)_messageBuffer).OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    }
}

