using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using VsTwitch.Twitch.WebSocket.Models;

namespace VsTwitch.Twitch.WebSocket
{
    internal class EventSubMessageFactory
    {
        private readonly JsonSerializerSettings jsonOptions;

        public EventSubMessageFactory()
        {
            DefaultContractResolver contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy(),
            };
            jsonOptions = new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                Formatting = Formatting.Indented,
            };
        }

        public EventSubMessageMetadata CreateMetadataFromData(string data)
        {
            return JsonConvert.DeserializeObject<EventSubMessageBase>(data, jsonOptions).Metadata;
        }

        public EventSubMessage<T> CreateMessageFromData<T>(string data)
        {
            return JsonConvert.DeserializeObject<EventSubMessage<T>>(data, jsonOptions);
        }

        public EventSubMessage<EventSubMessageNotification<T>> CreateNotificationFromData<T>(string data)
        {
            return CreateMessageFromData<EventSubMessageNotification<T>>(data);
        }
    }
}
