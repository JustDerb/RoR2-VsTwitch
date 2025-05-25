using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace VsTwitch.Twitch.WebSocket.Models
{
    internal class EventSubMessageNotificationSubscriptionTransport
    {
        [JsonProperty("method")]
        public string Method { get; set; }
        [JsonProperty("session_id")]
        public string SessionId { get; set; }
    }

    internal class EventSubMessageNotificationSubscription
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("cost")]
        public int Cost { get; set; }
        [JsonProperty("condition")]
        public Dictionary<string, string> Condition { get; set; }
        [JsonProperty("transport")]
        public EventSubMessageNotificationSubscriptionTransport Transport { get; set; }
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    internal class EventSubMessageNotification<T>
    {
        [JsonProperty("subscription")]
        public EventSubMessageNotificationSubscription Subscription { get; set; }
        [JsonProperty("event")]
        public T Event { get; set; }
    }
}
