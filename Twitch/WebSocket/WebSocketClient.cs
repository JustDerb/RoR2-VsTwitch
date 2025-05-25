using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VsTwitch.Twitch.WebSocket.Models;

namespace VsTwitch.Twitch.WebSocket
{
    internal class WebSocketConnectedArgs
    {
        public bool IsRequestedReconnect { get; set; }
    }

    internal class WebSocketClient : IDisposable
    {
        public const string EVENTSUB_ENDPOINT = "wss://eventsub.wss.twitch.tv/ws";
        private const int MAX_BUFFER_SIZE = 2 ^ 16;

        private readonly ClientWebSocket webSocket;
        //private readonly BlockingCollection<string> sendQueue;
        private readonly Uri endpoint;
        private readonly EventSubMessageFactory eventSubMessageFactory;
        private TimeSpan keepAliveTimeout = TimeSpan.Zero;
        private Dictionary<string, NotificationHandler> notificationHandler;

        public string SessionId { get; private set; }

        public event EventHandler<string> OnLog;
        public event EventHandler<WebSocketConnectedArgs> WebsocketConnected;
        public event EventHandler WebsocketDisconnected;

        public WebSocketClient(string endpoint, EventSubMessageFactory? messageFactory)
        {
            this.endpoint = new Uri(endpoint ?? EVENTSUB_ENDPOINT);
            webSocket = new ClientWebSocket();
            webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(2);
            //sendQueue = new BlockingCollection<string>();
            eventSubMessageFactory = messageFactory ?? new EventSubMessageFactory();
            SessionId = "";
            notificationHandler = new Dictionary<string, NotificationHandler>();
        }

        public bool IsConnected => webSocket.State == WebSocketState.Open;

        private string GetHandlerId(string type, string version)
        {
            return $"{type}-{version}";
        }

        public void RegisterHandlers(params NotificationHandler[] handlers)
        {
            foreach (NotificationHandler h in handlers)
            {
                notificationHandler[GetHandlerId(h.GetNotificationType(), h.GetNotificationVersion())] = h;
            }
        }

        private bool HandleNotification(string subscriptionType, string subscriptionVersion, string message)
        {
            try
            {
                if (notificationHandler.TryGetValue(GetHandlerId(subscriptionType, subscriptionVersion), out NotificationHandler handler))
                {
                    handler.HandleNotification(message);
                    return true;
                }
            }
            catch (Exception e)
            {
                OnLog?.Invoke(this, $"[{GetHandlerId(subscriptionType, subscriptionVersion)}] Exception: {e}");
            }
            return false;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            await webSocket.ConnectAsync(endpoint, cancellationToken);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () => await ReceiveDataAsync(cancellationToken));
            //Task.Run(async () => await SendDataAsync(cancellationToken));
            // FIXME: Keepalive timer
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                webSocket.Abort();
                return Task.CompletedTask;
            }
            return webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken);
        }

        private async Task ReceiveDataAsync(CancellationToken cancellationToken)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[MAX_BUFFER_SIZE]);

            while (IsConnected || cancellationToken.IsCancellationRequested)
            {
                using MemoryStream fullMessage = new MemoryStream();
                WebSocketReceiveResult receiveResult;
                do
                {
                    receiveResult = await webSocket.ReceiveAsync(buffer, cancellationToken);

#pragma warning disable CS8604 // Possible null reference argument.
                    fullMessage.Write(buffer.Array, buffer.Offset, receiveResult.Count);
#pragma warning restore CS8604 // Possible null reference argument.
                } while (!receiveResult.EndOfMessage);

                // Rewind read/write pointer
                fullMessage.Seek(0, SeekOrigin.Begin);

                switch (receiveResult.MessageType)
                {
                    case WebSocketMessageType.Text:
                        {
                            using StreamReader reader = new StreamReader(fullMessage, Encoding.UTF8);
                            string message = reader.ReadToEnd();
                            OnLog?.Invoke(this, message);

                            EventSubMessageMetadata eventSubMessage = eventSubMessageFactory.CreateMetadataFromData(message);

                            HandleTextMessage(eventSubMessage, message);
                            break;
                        }
                    case WebSocketMessageType.Binary:
                        break;
                    case WebSocketMessageType.Close:
                        WebsocketDisconnected?.Invoke(this, null);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void HandleTextMessage(EventSubMessageMetadata eventSubMessage, string message)
        {
            OnLog?.Invoke(this, $"metadata.message_type: {eventSubMessage.MessageType}");
            switch (eventSubMessage.MessageType)
            {
                case "session_welcome":
                    var sessionMessage = eventSubMessageFactory.CreateMessageFromData<WebSocketSessionMessage>(message);

                    SessionId = sessionMessage.Payload.Session.Id;
                    keepAliveTimeout = TimeSpan.FromSeconds(Math.Max(sessionMessage.Payload.Session.KeepaliveTimeoutSeconds, 10));
                    WebsocketConnected?.Invoke(this, new WebSocketConnectedArgs()
                    {
                        IsRequestedReconnect = false,
                    });
                    break;
                case "session_disconnect":
                    break;
                case "session_reconnect":
                    // TODO
                    break;
                case "session_keepalive":
                    break;
                case "notification":
                    if (string.IsNullOrWhiteSpace(eventSubMessage.SubscriptionType))
                    {
                        OnLog?.Invoke(this, "notification: metadata.subscription_type is empty!");
                        break;
                    }
                    if (string.IsNullOrWhiteSpace(eventSubMessage.SubscriptionVersion))
                    {
                        OnLog?.Invoke(this, "notification: metadata.subscription_version is empty!");
                        break;
                    }
                    bool handled = HandleNotification(eventSubMessage.SubscriptionType, eventSubMessage.SubscriptionVersion, message);
                    if (!handled)
                    {
                        OnLog?.Invoke(this, $"{GetHandlerId(eventSubMessage.SubscriptionType, eventSubMessage.SubscriptionVersion)} failed");
                    }
                    break;
                case "revocation":
                    break;
                default:
                    OnLog?.Invoke(this, $"Unknown metadata.message_type: {eventSubMessage.MessageType}");
                    break;
            }
        }

        //public void SendMessage(string message)
        //{
        //    sendQueue.Add(message);
        //}

        //private async Task SendDataAsync(CancellationToken cancellationToken)
        //{
        //    while (IsConnected || cancellationToken.IsCancellationRequested)
        //    {
        //        var data = sendQueue.Take(cancellationToken);
        //        if (!IsConnected) {
        //            break;
        //        }

        //        // FIXME: Frame the data
        //        ArraySegment<byte> buffer = new ArraySegment<byte>(UTF8Encoding.UTF8.GetBytes(data));
        //        await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
        //    }
        //}

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            webSocket.Dispose();
            //sendQueue.Dispose();
        }
    }
}
