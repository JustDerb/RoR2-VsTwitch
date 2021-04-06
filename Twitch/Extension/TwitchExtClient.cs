using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VsTwitch
{
    class OnConnectedEventArgs : EventArgs
    {
        public Uri Uri { get; set; }
    }
    class OnDisconnectedEventArgs : EventArgs
    {
        public bool ShouldRetry { get; set; }
    }
    class OnErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
        public Exception Exception { get; set; }

        public override string ToString()
        {
            return $"OnErrorEventArgs: {Message}\n{Exception}";
        }
    }
    class OnMessageEventArgs : EventArgs
    {
        public string Nonce { get; set; }

        public string Message { get; set; }
        public override string ToString()
        {
            return $"OnMessageEventArgs: {Nonce}\n{Message}";
        }
    }

    class ExtensionMessageRequest
    {
        public string Nonce { get; set; }

        public string Message { get; set; }
    }

    class TwitchExtClient : IDisposable
    {
        public event EventHandler<OnConnectedEventArgs> OnConnected;
        public event EventHandler<OnDisconnectedEventArgs> OnDisconnected;
        public event EventHandler<OnErrorEventArgs> OnError;
        public event EventHandler<OnMessageEventArgs> OnMessage;

        private ClientWebSocket webSocket;
        private CancellationTokenSource webSocketToken;
        private Task[] tasks;

        public TwitchExtClient()
        {
        }

        public void Connect(Uri uri)
        {
            if (IsConnected()) {
                return;
            }

            Disconnect();

            webSocket = new ClientWebSocket();
            webSocketToken = new CancellationTokenSource();
            if (!uri.Scheme.StartsWith("ws"))
            {
                throw new ArgumentException("uri must be of schema ws:// or wss://");
            }
            Task task = webSocket.ConnectAsync(uri, webSocketToken.Token);
            task.Wait(10000);
            if (!IsConnected())
            {
                throw new InvalidOperationException($"Couldn't connect to {uri}");
            }

            tasks = new[]
            {
                StartListenerTask(),
            };

            // TODO: Check .IsFaulted

            OnConnected?.Invoke(this, new OnConnectedEventArgs() { Uri = uri });
        }

        public bool SendMessage(ExtensionMessageRequest message)
        {
            if (!IsConnected())
            {
                return false;
            }

            if (message.Nonce == null || message.Nonce == "")
            {
                message.Nonce = Guid.NewGuid().ToString();
            }

            string messageJson = JsonConvert.SerializeObject(message);
            // FIXME: Wait on message being sent??
            webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(messageJson)), WebSocketMessageType.Text, true, webSocketToken.Token);
            return true;
        }

        private Task StartListenerTask()
        {
            return Task.Run(async () =>
            {
                string message = "";

                while (IsConnected())
                {
                    WebSocketReceiveResult result;
                    var buffer = new byte[1024];

                    try
                    {
                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), webSocketToken.Token);
                    }
                    catch
                    {
                        break;
                    }

                    if (result == null) continue;

                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Close:
                            Disconnect();
                            break;
                        case WebSocketMessageType.Text when !result.EndOfMessage:
                            message += Encoding.UTF8.GetString(buffer).TrimEnd('\0');
                            continue;
                        case WebSocketMessageType.Text:
                            message += Encoding.UTF8.GetString(buffer).TrimEnd('\0');
                            try
                            {
                                OnMessage?.Invoke(this, ParseMessage(message));
                            }
                            catch (Exception ex)
                            {
                                OnError?.Invoke(this, new OnErrorEventArgs() {
                                    Message = message,
                                    Exception = ex,
                                });
                            }
                            break;
                        case WebSocketMessageType.Binary:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    message = "";
                }
            });
        }

        private OnMessageEventArgs ParseMessage(string message)
        {
            return JsonConvert.DeserializeObject<OnMessageEventArgs>(message);
        }

        public bool IsConnected()
        {
            return webSocket?.State == WebSocketState.Open;
        }

        public void Disconnect()
        {
            if (webSocket == null)
            {
                return;
            }

            webSocketToken.Cancel();
            webSocket.Abort();
            webSocket = null;
            webSocketToken = null;

            OnDisconnected?.Invoke(this, new OnDisconnectedEventArgs());
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
