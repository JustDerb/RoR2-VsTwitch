using Newtonsoft.Json;

namespace VsTwitch.Twitch.WebSocket.Models
{
    internal class EventSubMessageBase
    {
        [JsonProperty("metadata")]
        public EventSubMessageMetadata Metadata { get; set; }
    }
}
