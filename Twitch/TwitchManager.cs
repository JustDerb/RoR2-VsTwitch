using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Events;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.Unity;
using UnityEngine;
using VsTwitch.Data;
using VsTwitch.Twitch;
using VsTwitch.Twitch.Auth;

namespace VsTwitch
{
    /// <summary>
    /// Small wrapper around <c>TwitchLib</c> to help organize Twitch events
    /// </summary>
    class TwitchManager
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly SetupHelper setupHelper;
        private AuthManager Auth = null;
        private EventSubWebSocket TwitchEventSubWebSocket = null;

        public bool DebugLogs { get; set; }

        public event AsyncEventHandler<OnMessageReceivedArgs> OnMessageReceived;
        public event AsyncEventHandler<ChannelPointsCustomRewardRedemption> OnRewardRedeemed;
        public event AsyncEventHandler<OnJoinedChannelArgs> OnConnected;
        public event AsyncEventHandler<OnDisconnectedArgs> OnDisconnected;

        public TwitchManager(SetupHelper setupHelper)
        {
            DebugLogs = false;
            loggerFactory = Log.CreateLoggerFactory((type, category, logLevel) => DebugLogs);
            this.setupHelper = setupHelper;
        }

        private async Task InitializeAuth()
        {
            if (Auth != null)
            {
                return;
            }
            string dataBasePath = Path.Combine(Application.persistentDataPath, VsTwitch.GUID);
            Log.Debug($"AuthManager::DataManager basepath = {dataBasePath}");
            DataManager dataManager = DataManager.LoadForModule(dataBasePath, "TwitchAuth");
            Auth = await AuthManager.Create(dataManager, loggerFactory);
        }

        public async Task MaybeSetup(Configuration config)
        {
            await Disconnect();
            LogDebug("TwitchManager::MaybeSetup");
            await InitializeAuth();
            if (!Auth.IsAuthed())
            {
                await setupHelper.GuideThroughTwitchIntegration(Auth, config);
            }
        }

        public async Task Connect()
        {
            await Disconnect();
            LogDebug("TwitchManager::Connect");
            LogDebug("[Twitch API] Logging in...");
            await InitializeAuth();

            await Auth.MaybeAuthUser();

            Auth.TwitchClient.OnJoinedChannel += OnConnected;
            Auth.TwitchClient.OnMessageReceived += OnMessageReceived;
            Auth.TwitchClient.OnConnected += (object sender, TwitchLib.Client.Events.OnConnectedEventArgs e) =>
            {
                Auth.TwitchClient.JoinChannelAsync(Auth.TwitchChannelId);
                return Task.CompletedTask;
            };
            Auth.TwitchClient.OnDisconnected += OnDisconnected;
            LogDebug("[Twitch Client] Connecting...");
            await Auth.TwitchClient.ConnectAsync();

            LogDebug("[Twitch EventSub | ] Creating...");
            TwitchEventSubWebSocket = new EventSubWebSocket(loggerFactory);
            TwitchEventSubWebSocket.WebsocketConnected += async (sender, e) =>
            {
                LogDebug($"[Twitch EventSub | {TwitchEventSubWebSocket.SessionId}] Connected!");

                if (!e.IsRequestedReconnect)
                {
                    LogDebug($"[Twitch EventSub | {TwitchEventSubWebSocket.SessionId}] Deleting all subscriptions for token...");
                    var response = await Auth.TwitchAPI.Helix.EventSub.GetEventSubSubscriptionsAsync();
                    Task<bool[]> allDeleted = Task.WhenAll(
                        response.Subscriptions.Select(
                            s => Auth.TwitchAPI.Helix.EventSub.DeleteEventSubSubscriptionAsync(s.Id)
                        )
                    );
                    await allDeleted;
                    // TODO: Maybe ensure all the return values are true?
                    LogDebug($"[Twitch EventSub | {TwitchEventSubWebSocket.SessionId}] Deleted {allDeleted.Result.Length} subscriptions.");

                    LogDebug($"[Twitch EventSub | {TwitchEventSubWebSocket.SessionId}] Requesting channel.channel_points_custom_reward_redemption.add (v1) subscription...");
                    // Subscribe to Channel Point Redeems
                    Dictionary<string, string> conditions = new Dictionary<string, string>()
                    {
                        { "broadcaster_user_id", Auth.TwitchChannelId }
                    };
                    CreateEventSubSubscriptionResponse ret = await Auth.TwitchAPI.Helix.EventSub.CreateEventSubSubscriptionAsync(
                        "channel.channel_points_custom_reward_redemption.add", "1", conditions,
                        EventSubTransportMethod.Websocket, TwitchEventSubWebSocket.SessionId);
                    LogDebug($"[Twitch EventSub | {TwitchEventSubWebSocket.SessionId}] Current subscriptions active: {ret.Total}");
                }
            };
            TwitchEventSubWebSocket.WebsocketDisconnected += (sender, e) =>
            {
                LogDebug($"[Twitch EventSub | {TwitchEventSubWebSocket.SessionId}] Disconnected!");
                return Task.CompletedTask;
            };
            TwitchEventSubWebSocket.WebsocketReconnected += (sender, e) =>
            {
                LogDebug($"[Twitch EventSub | {TwitchEventSubWebSocket.SessionId}] Reconnected!");
                return Task.CompletedTask;
            };
            TwitchEventSubWebSocket.ErrorOccurred += (sender, e) =>
            {
                Log.Error($"[Twitch EventSub | {TwitchEventSubWebSocket.SessionId}] ERROR: {e.Message} ({e.Exception})");
                return Task.CompletedTask;
            };
            TwitchEventSubWebSocket.ChannelPointsCustomRewardRedemptionAdd += (sender, e) =>
            {
                // Wrap this to only serialize JSON if debug logs are enabled
                if (DebugLogs)
                {
                    LogDebug($"[Twitch EventSub | {TwitchEventSubWebSocket.SessionId}] Channel Point Redeemed: {JsonConvert.SerializeObject(e.Notification.Payload.Event)}");
                }
                if (OnRewardRedeemed != null)
                {
                    return OnRewardRedeemed.Invoke(this, e.Notification.Payload.Event);
                }
                return Task.CompletedTask;
            };
            await TwitchEventSubWebSocket.ConnectAsync();
        }

        public async Task Disconnect()
        {
            LogDebug("TwitchManager::Disconnect");
            if (TwitchEventSubWebSocket != null)
            {
                try
                {
                    await TwitchEventSubWebSocket.DisconnectAsync();
                }
                catch (Exception e)
                {
                    // We don't care about any exceptions here, we're tearing down.
                    Log.Debug(e);
                }
                TwitchEventSubWebSocket = null;
            }
            if (Auth != null)
            {
                try
                {
                    await Auth.TwitchClient.DisconnectAsync();
                }
                catch (Exception e)
                {
                    // We don't care about any exceptions here, we're tearing down.
                    Log.Debug(e);
                }
                Auth = null;
            }
        }

        public bool IsConnected()
        {
            return Auth.TwitchClient != null && Auth.TwitchClient.IsConnected && Auth.IsAuthed();
        }

        public void SendMessage(string message)
        {
            if (!IsConnected())
            {
                Log.Warning("[Twitch Client] Not connected to Twitch!");
                return;
            }
            Auth.TwitchClient.SendMessageAsync(Auth.TwitchUsername, message);
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
