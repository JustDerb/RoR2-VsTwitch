
using System;

namespace VsTwitch.Twitch.WebSocket
{
    internal abstract class NotificationHandler
    {
        protected readonly string type;
        protected readonly string version;
        protected readonly EventSubMessageFactory factory;

        public NotificationHandler(string type, string version, EventSubMessageFactory factory)
        {
            this.type = type;
            this.version = version;
            this.factory = factory;
        }

        public string GetNotificationType()
        {
            return type;
        }

        public string GetNotificationVersion()
        {
            return version;
        }

        public abstract void HandleNotification(string message);
    }
}
