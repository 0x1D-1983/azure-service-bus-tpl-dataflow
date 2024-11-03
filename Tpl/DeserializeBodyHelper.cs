using Azure.Messaging.ServiceBus;
using TplAzureServiceBusWorker.Models;

namespace TplAzureServiceBusWorker.Tpl
{
	public class DeserializeBodyHelper
	{
        public static Func<ServiceBusReceivedMessage, Tuple<ServiceBusReceivedMessage, Item?>> MapPurchase() => input =>
            new Tuple<ServiceBusReceivedMessage, Item?>(input, input.As());
    }
}

