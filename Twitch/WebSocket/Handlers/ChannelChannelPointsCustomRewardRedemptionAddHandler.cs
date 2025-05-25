using System;
using TwitchLib.Unity;
using VsTwitch.Twitch.WebSocket.Models.Notifications;

namespace VsTwitch.Twitch.WebSocket.Handlers
{
    internal class ChannelChannelPointsCustomRewardRedemptionAddHandler : NotificationHandler
    {
        public event EventHandler<ChannelPointsCustomRewardRedemptionAddMessage> OnEvent;

        public ChannelChannelPointsCustomRewardRedemptionAddHandler(EventSubMessageFactory factory) : base("channel.channel_points_custom_reward_redemption.add", "1", factory)
        {
        }

        public override void HandleNotification(string message)
        {
            var notif = factory.CreateNotificationFromData<ChannelPointsCustomRewardRedemptionAddMessage>(message);
            // FIXME: Migrate to internal ThreadDispatcher-like object
            ThreadDispatcher.EnsureCreated();
            ThreadDispatcher.Enqueue(() => OnEvent?.Invoke(this, notif.Payload.Event));
        }
    }
}
