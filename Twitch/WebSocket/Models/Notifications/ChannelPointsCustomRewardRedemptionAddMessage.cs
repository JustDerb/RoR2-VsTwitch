using System;

namespace VsTwitch.Twitch.WebSocket.Models.Notifications
{
    internal class ChannelPointsCustomRewardRedemptionAddMessageRedemptionReward
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Cost { get; set; }
        public string Prompt { get; set; } = string.Empty;
    }

    internal class ChannelPointsCustomRewardRedemptionAddMessage
    {
        public string Id { get; set; } = string.Empty;
        public string BroadcasterUserId { get; set; } = string.Empty;
        public string BroadcasterUserName { get; set; } = string.Empty;
        public string BroadcasterUserLogin { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserLogin { get; set; } = string.Empty;
        public string UserInput { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public ChannelPointsCustomRewardRedemptionAddMessageRedemptionReward Reward { get; set; } = new();
        public DateTimeOffset RedeemedAt { get; set; } = DateTimeOffset.MinValue;
    }
}
