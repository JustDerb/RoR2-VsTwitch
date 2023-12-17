using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;
using TwitchLib.PubSub.Events;
using TwitchLib.Unity;

namespace VsTwitch
{
    /// <summary>
    /// Small wrapper around <c>TwitchLib</c> to help organize Twitch events
    /// </summary>
    class TwitchManager
    {
        private Client TwitchClient = null;
        private Api TwitchApi = null;
        private PubSub TwitchPubSub = null;
        private string Channel;
        public string Username { get; private set; }

        public bool DebugLogs { get; set; }

        public event EventHandler<OnMessageReceivedArgs> OnMessageReceived;
        public event EventHandler<OnRewardRedeemedArgs> OnRewardRedeemed;
        public event EventHandler<OnJoinedChannelArgs> OnConnected;
        public event EventHandler<OnDisconnectedEventArgs> OnDisconnected;

        public TwitchManager()
        {
            DebugLogs = false;
        }

        public void Connect(string channel, string oauthToken, string username, string clientId)
        {
            Disconnect();
            LogDebug("TwitchManager::Connect");

            if (channel == null || channel.Trim().Length == 0)
            {
                throw new ArgumentException("Twitch channel must be specified!", "channel");
            }
            if (oauthToken == null || oauthToken.Trim().Length == 0)
            {
                throw new ArgumentException("Twitch OAuth password must be specified!", "oauthToken");
            }
            if (username == null || username.Trim().Length == 0)
            {
                throw new ArgumentException("Twitch username must be specified!", "username");
            }

            Channel = channel;
            Username = username;

            LogDebug("[Twitch API] Creating...");
            TwitchApi = new Api();
            string twitchApiOauthToken = oauthToken;
            if (twitchApiOauthToken.StartsWith("oauth:"))
            {
                twitchApiOauthToken = twitchApiOauthToken.Substring("oauth:".Length);
            }
            TwitchApi.Settings.AccessToken = twitchApiOauthToken;
            TwitchApi.Settings.ClientId = clientId;
            string channelId = null;
            try
            {
                LogDebug("[Twitch API] Trying to find channel ID...");
                Task<TwitchLib.Api.Helix.Models.Users.GetUsersResponse> response = TwitchApi.Helix.Users.GetUsersAsync(null,
                    new List<string>(new string[] { channel }));
                response.Wait();

                if (response.Result.Users.Length == 1)
                {
                    channelId = response.Result.Users[0].Id;
                    Log.Info($"[Twitch API] Channel ID for {channel} = {channelId}");
                }
                else
                {
                    throw new ArgumentException($"Couldn't find Twitch user/channel {channel}!");
                }
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException)
                {
                    throw ex;
                }
                Log.Exception(ex);
            }

            LogDebug("[Twitch Client] Creating...");
            ConnectionCredentials credentials = new ConnectionCredentials(username, oauthToken);
            TwitchClient = new Client();
            TwitchClient.Initialize(credentials, channel);
            TwitchClient.OnLog += TwitchClient_OnLog;
            TwitchClient.OnJoinedChannel += OnConnected;
            TwitchClient.OnMessageReceived += OnMessageReceived;
            TwitchClient.OnConnected += TwitchClient_OnConnected;
            TwitchClient.OnDisconnected += OnDisconnected;
            LogDebug("[Twitch Client] Connecting...");
            TwitchClient.Connect();

            if (channelId != null && channelId.Trim().Length != 0)
            {
                LogDebug("[Twitch PubSub] Creating...");
                TwitchPubSub = new PubSub();
                TwitchPubSub.OnLog += TwitchPubSub_OnLog;
                TwitchPubSub.OnPubSubServiceConnected += (sender, e) =>
                {
                    Log.Info("[Twitch PubSub] Sending topics to listen too...");
                    TwitchPubSub.ListenToRewards(channelId);
                    TwitchPubSub.SendTopics(twitchApiOauthToken);
                };
                TwitchPubSub.OnPubSubServiceError += (sender, e) =>
                {
                    Log.Error($"[Twitch PubSub] ERROR: {e.Exception}");
                };
                TwitchPubSub.OnPubSubServiceClosed += (sender, e) =>
                {
                    Log.Info($"[Twitch PubSub] Connection closed");
                };
                TwitchPubSub.OnListenResponse += (sender, e) =>
                {
                    if (!e.Successful)
                    {
                        Log.Error($"[Twitch PubSub] Failed to listen! Response: {e.Response}");
                    }
                    else
                    {
                        Log.Info($"[Twitch PubSub] Listening to {e.Topic} - {e.Response}");
                    }
                };
                TwitchPubSub.OnRewardRedeemed += OnRewardRedeemed;
                Log.Info("[Twitch PubSub] Connecting...");
                TwitchPubSub.Connect();
            }
        }

        public void Disconnect()
        {
            LogDebug("TwitchManager::Disconnect");
            if (TwitchClient != null)
            {
                TwitchClient.Disconnect();
                TwitchClient = null;
            }
            if (TwitchPubSub != null)
            {
                TwitchPubSub.Disconnect();
                TwitchPubSub = null;
            }
            if (TwitchApi != null)
            {
                TwitchApi = null;
            }
        }

        public bool IsConnected()
        {
            return TwitchClient != null && TwitchClient.IsConnected;
        }

        public void SendMessage(string message)
        {
            if (!IsConnected())
            {
                Log.Warning("[Twitch Client] Not connected to Twitch!");
                return;
            }
            TwitchClient.SendMessage(Channel, message);
        }

        private void TwitchClient_OnConnected(object sender, OnConnectedArgs e)
        {
            Log.Info("[Twitch Client] Connected to Twitch using username: " + e.BotUsername);
        }

        private void TwitchPubSub_OnLog(object sender, TwitchLib.PubSub.Events.OnLogArgs e)
        {
            LogDebug($"[Twitch PubSub] {e.Data}");
        }

        private void TwitchClient_OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        {
            LogDebug($"[Twitch Client] {e.DateTime}: {e.BotUsername} - {e.Data}");
        }

        private void LogDebug(string message)
        {
            if (DebugLogs)
            {
                Log.Debug(message);
            }
        }
    }
}
