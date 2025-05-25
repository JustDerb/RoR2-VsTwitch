using Newtonsoft.Json;
using System;

namespace VsTwitch.Twitch.WebSocket.Models
{
    internal class EventSubMessageMetadata
    {
        [JsonProperty("message_id")]
        public string MessageId { get; set; }
        [JsonProperty("message_type")]
        public string MessageType { get; set; }
        [JsonProperty("message_timestamp")]
        public DateTime MessageTimestamp { get; set; }
        [JsonProperty("subscription_type")]
        public string? SubscriptionType { get; set; }
        [JsonProperty("subscription_version")]
        public string? SubscriptionVersion { get; set; }
    }
}
