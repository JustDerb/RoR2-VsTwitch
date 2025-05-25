using Newtonsoft.Json;

namespace VsTwitch.Twitch.WebSocket.Models
{
    internal class EventSubMessage<T> : EventSubMessageBase
    {
        [JsonProperty("payload")]
        public T Payload { get; set; }
    }
}
