using System;
using TwitchLib.Unity;
using VsTwitch.Twitch.WebSocket.Models.Notifications;

namespace VsTwitch.Twitch.WebSocket.Handlers
{
    internal class ChannelChatMessageHandler : NotificationHandler
    {
        public event EventHandler<ChannelChatMessage> OnEvent;

        public ChannelChatMessageHandler(EventSubMessageFactory factory) : base("channel.chat.message", "1", factory)
        {
        }

        public override void HandleNotification(string message)
        {
            var notif = factory.CreateNotificationFromData<ChannelChatMessage>(message);
            // FIXME: Migrate to internal ThreadDispatcher-like object
            ThreadDispatcher.EnsureCreated();
            ThreadDispatcher.Enqueue(() => OnEvent?.Invoke(this, notif.Payload.Event));
        }
    }
}
