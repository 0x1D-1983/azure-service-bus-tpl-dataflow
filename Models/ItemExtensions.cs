using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace TplAzureServiceBusWorker.Models
{
	public static class ItemExtensions
	{
        public static Item? As(this ServiceBusReceivedMessage message)
        {
            // Convert message body to string
            string receivedJson = message.Body.ToString();

            // Deserialize
            return JsonSerializer.Deserialize<Item>(receivedJson);
        }

        public static ServiceBusMessage AsMessage(this Item obj)
        {
            // Serialize to JSON string
            string jsonString = JsonSerializer.Serialize(obj);

            var message = new ServiceBusMessage(jsonString)
            {
                Subject = "Store Item",
                ContentType = "application/json"
            };

            return message;
        }

        public static bool Any(this IList<ServiceBusReceivedMessage> collection)
        {
            return collection != null && collection.Count > 0;
        }

        public static string PrintMessage(this Item item, string storeId) => $"Store: {storeId}, Price={item.Price}, Color={item.Color}, Category={item.Category}";
    }
}

