using System;
using System.Linq;

namespace VsTwitch.Twitch.WebSocket.Models.Notifications
{
    internal class ChannelChatMessage
    {
        public string BroadcasterUserId { get; set; } = string.Empty;
        public string BroadcasterUserName { get; set; } = string.Empty;
        public string BroadcasterUserLogin { get; set; } = string.Empty;
        public string ChatterUserId { get; set; } = string.Empty;
        public string ChatterUserName { get; set; } = string.Empty;
        public string ChatterUserLogin { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public ChatMessage Message { get; set; } = new();
        public string Color { get; set; } = string.Empty;
        public ChatBadge[] Badges { get; set; }

        public string MessageType { get; set; } = string.Empty;

        public ChatCheer? Cheer { get; set; }

        //public ChatReply? Reply { get; set; }

        public string ChannelPointsCustomRewardId { get; set; } = string.Empty;

        public bool IsSubscriber => Badges.Any(x => x.SetId.Equals("subscriber", StringComparison.OrdinalIgnoreCase));
        public bool IsModerator => Badges.Any(x => x.SetId.Equals("moderator", StringComparison.OrdinalIgnoreCase));
        public bool IsBroadcaster => Badges.Any(x => x.SetId.Equals("broadcaster", StringComparison.OrdinalIgnoreCase));
        public bool IsVip => Badges.Any(x => x.SetId.Equals("vip", StringComparison.OrdinalIgnoreCase));
    }

    public class ChatBadge
    {
        public string SetId { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Info { get; set; } = string.Empty;
    }

    internal class ChatMessage
    {
        public string Text { get; set; } = string.Empty;
        //public ChatMessageFragment[] Fragments { get; set; } = Array.Empty<ChatMessageFragment>();
    }

    internal class ChatCheer
    {
        public int Bits { get; set; }
    }
}
