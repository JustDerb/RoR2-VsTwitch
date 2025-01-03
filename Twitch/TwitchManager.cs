using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Events;
using TwitchLib.PubSub.Events;
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
        private PubSub TwitchPubSub = null;

        public bool DebugLogs { get; set; }

        public event AsyncEventHandler<OnMessageReceivedArgs> OnMessageReceived;
        public event EventHandler<OnChannelPointsRewardRedeemedArgs> OnRewardRedeemed;
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

            LogDebug("[Twitch PubSub] Creating...");
            TwitchPubSub = new PubSub(loggerFactory.CreateLogger<PubSub>());
            TwitchPubSub.OnLog += TwitchPubSub_OnLog;
            TwitchPubSub.OnPubSubServiceConnected += (sender, e) =>
            {
                Log.Info("[Twitch PubSub] Sending topics to listen too...");
                TwitchPubSub.ListenToChannelPoints(Auth.TwitchChannelId);
                TwitchPubSub.ListenToBitsEventsV2(Auth.TwitchChannelId);
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
            await TwitchPubSub.ConnectAsync();
        }

        public async Task Disconnect()
        {
            LogDebug("TwitchManager::Disconnect");
            if (TwitchPubSub != null)
            {
                try
                {
                    await TwitchPubSub.DisconnectAsync();
                }
                catch (Exception e)
                {
                    // We don't care about any exceptions here, we're tearing down.
                    Log.Debug(e);
                }
                TwitchPubSub = null;
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
