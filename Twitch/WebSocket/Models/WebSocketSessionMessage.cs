using Newtonsoft.Json;
using System;

namespace VsTwitch.Twitch.WebSocket.Models
{
    internal class WebSocketSessionMessageInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("connected_at")]
        public DateTime ConnectedAt { get; set; }
        [JsonProperty("keepalive_timeout_seconds")]
        public int KeepaliveTimeoutSeconds { get; set; }
        [JsonProperty("reconnect_url")]
        public string? ReconnectUrl { get; set; }
    }

    internal class WebSocketSessionMessage
    {
        [JsonProperty("session")]
        public WebSocketSessionMessageInfo Session { get; set; }
    }
}
