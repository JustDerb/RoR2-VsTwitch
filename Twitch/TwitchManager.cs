using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Newtonsoft.Json;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;
using TwitchLib.PubSub.Events;
using TwitchLib.Unity;
using UnityEngine;
using VsTwitch.Twitch.Auth;

namespace VsTwitch
{
    /// <summary>
    /// Small wrapper around <c>TwitchLib</c> to help organize Twitch events
    /// </summary>
    class TwitchManager
    {
        private Client TwitchClient = null;
        //private Api TwitchApi = null;
        private AuthManager Auth = null;
        private PubSub TwitchPubSub = null;
        private string Channel;
        public string Username { get; private set; }

        public bool DebugLogs { get; set; }

        public event AsyncEventHandler<OnMessageReceivedArgs> OnMessageReceived;
        public event EventHandler<OnChannelPointsRewardRedeemedArgs> OnRewardRedeemed;
        public event AsyncEventHandler<OnJoinedChannelArgs> OnConnected;
        public event AsyncEventHandler<OnDisconnectedArgs> OnDisconnected;

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
            ILoggerFactory loggerFactory = Log.CreateLoggerFactory((type, category, logLevel) => DebugLogs);
            // TwitchApi = new Api();
            Auth = new AuthManager("", loggerFactory);
            //string twitchApiOauthToken = oauthToken;
            //if (twitchApiOauthToken.StartsWith("oauth:"))
            //{
            //    twitchApiOauthToken = twitchApiOauthToken.Substring("oauth:".Length);
            //}
            //TwitchApi.Settings.AccessToken = twitchApiOauthToken;
            //TwitchApi.Settings.ClientId = clientId;
            LogDebug("Authing...");
            Auth.MaybeAuthUser().GetAwaiter().GetResult();
            string channelId = null;
            LogDebug("[Twitch API] Trying to find channel ID...");
            Task<TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse> response = Auth.TwitchAPI.Helix.Users.GetUsersAsync(null,
                new List<string>(new string[] { channel }));
            response.Wait(5000);

            if (response.IsCompleted && response.Result.Users.Length == 1)
            {
                channelId = response.Result.Users[0].Id;
                Log.Info($"[Twitch API] Channel ID for {channel} = {channelId}");
            }

            if (channelId == null)
            {
                throw new ArgumentException($"Couldn't find Twitch user/channel {channel}!");
            }
            
            LogDebug("[Twitch Client] Creating...");
            ConnectionCredentials credentials = new ConnectionCredentials(username, oauthToken);
            TwitchClient = new Client();
            TwitchClient.Initialize(credentials, channel);
            //TwitchClient.OnLog += TwitchClient_OnLog;
            TwitchClient.OnJoinedChannel += OnConnected;
            TwitchClient.OnMessageReceived += OnMessageReceived;
            TwitchClient.OnConnected += (object sender, TwitchLib.Client.Events.OnConnectedEventArgs e) =>
            {
                TwitchClient.JoinChannelAsync(channelId);
                return Task.CompletedTask;
            };
            TwitchClient.OnDisconnected += OnDisconnected;
            LogDebug("[Twitch Client] Connecting...");
            TwitchClient.ConnectAsync();

            if (channelId != null && channelId.Trim().Length != 0)
            {
                ILogger<PubSub> logger = loggerFactory.CreateLogger<PubSub>();
                logger.LogError("Created internal Twitch PubSub logger");

                LogDebug("[Twitch PubSub] Creating...");
                TwitchPubSub = new PubSub(logger);
                TwitchPubSub.OnLog += TwitchPubSub_OnLog;
                TwitchPubSub.OnPubSubServiceConnected += (sender, e) =>
                {
                    Log.Info("[Twitch PubSub] Sending topics to listen too...");
                    TwitchPubSub.ListenToChannelPoints(channelId);
                    TwitchPubSub.ListenToBitsEventsV2(channelId);
                    TwitchPubSub.SendTopicsAsync(Auth.TwitchAPI.Settings.AccessToken);
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
                        Log.Error($"[Twitch PubSub] Failed to listen! Response: {JsonConvert.SerializeObject(e.Response)}");
                    }
                    else
                    {
                        Log.Info($"[Twitch PubSub] Listening to {e.Topic} - {JsonConvert.SerializeObject(e.Response)}");
                    }
                };
                // ListenToChannelPoints
                TwitchPubSub.OnChannelPointsRewardRedeemed += OnRewardRedeemed;
                // ListenToBitsEventsV2 - This is taken care of automatically via the "OnMessageReceived" event
                // TwitchPubSub.OnBitsReceivedV2 += OnBitsReceivedV2;
                Log.Info("[Twitch PubSub] Connecting...");
                TwitchPubSub.ConnectAsync();
            }
        }

        public void Disconnect()
        {
            LogDebug("TwitchManager::Disconnect");
            if (TwitchPubSub != null)
            {
                TwitchPubSub.DisconnectAsync();
                TwitchPubSub = null;
            }
            if (TwitchClient != null)
            {
                TwitchClient.DisconnectAsync();
                TwitchClient = null;
            }
            //if (TwitchApi != null)
            //{
            //    TwitchApi = null;
            //}
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
            TwitchClient.SendMessageAsync(Channel, message);
        }

        private void TwitchPubSub_OnLog(object sender, TwitchLib.PubSub.Events.OnLogArgs e)
        {
            LogDebug($"[Twitch PubSub] {e.Data}");
        }

        //private void TwitchClient_OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        //{
        //    LogDebug($"[Twitch Client] {e.DateTime}: {e.BotUsername} - {e.Data}");
        //}

        private void LogDebug(string message)
        {
            if (DebugLogs)
            {
                Log.Debug(message);
            }
        }
    }
}
