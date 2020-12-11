using System;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;
using TwitchLib.Unity;

namespace VsTwitch
{
    /// <summary>
    /// Small wrapper around <c>TwitchLib</c> to help organize Twitch events
    /// </summary>
    class TwitchManager
    {
        private Client TwitchClient = null;
        //private PubSub TwitchPubSub = null;
        private string Channel;
        public string Username { get; private set; }

        public bool DebugLogs { get; set; }

        public event EventHandler<OnMessageReceivedArgs> OnMessageReceived;
        public event EventHandler<OnJoinedChannelArgs> OnConnected;
        public event EventHandler<OnDisconnectedEventArgs> OnDisconnected;

        public TwitchManager()
        {
            DebugLogs = false;
        }

        public void Connect(string channel, string oauthToken, string username)
        {
            Disconnect();

            if (channel == null | channel.Trim().Length == 0)
            {
                throw new ArgumentException("Twitch channel must be specified!", "channel");
            }
            if (oauthToken == null | oauthToken.Trim().Length == 0)
            {
                throw new ArgumentException("Twitch OAuth password must be specified!", "oauthToken");
            }
            if (username == null | username.Trim().Length == 0)
            {
                throw new ArgumentException("Twitch username must be specified!", "username");
            }

            Channel = channel;
            Username = username;

            ConnectionCredentials credentials = new ConnectionCredentials(username, oauthToken);
            TwitchClient = new Client();
            //TwitchPubSub = new PubSub();

            TwitchClient.Initialize(credentials, channel);
            TwitchClient.OnLog += TwitchClient_OnLog;
            TwitchClient.OnJoinedChannel += OnConnected;
            TwitchClient.OnMessageReceived += OnMessageReceived;
            TwitchClient.OnConnected += TwitchClient_OnConnected;
            TwitchClient.OnDisconnected += OnDisconnected;

            //TwitchPubSub.OnPubSubServiceConnected += (sender, e) =>
            //{
            //    Console.WriteLine("Sending topics to listen too...");
            //    TwitchPubSub.SendTopics(oauthToken);
            //};
            //TwitchPubSub.OnListenResponse += (sender, e) =>
            //{
            //    if (!e.Successful)
            //    {
            //        Console.WriteLine($"Failed to listen! Response: {e.Response}");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"Listening to {e.Topic} - {e.Response}");
            //    }
            //};
            //TwitchPubSub.OnRewardRedeemed += TwitchPubSub_OnRewardRedeemed;

            // TwitchPubSub.ListenToRewards();
            TwitchClient.Connect();
            //TwitchPubSub.Connect();
        }

        private void TwitchPubSub_OnRewardRedeemed(object sender, TwitchLib.PubSub.Events.OnRewardRedeemedArgs e)
        {
            Console.WriteLine(e);
        }

        public void Disconnect()
        {
            if (TwitchClient != null)
            {
                TwitchClient.Disconnect();
                TwitchClient = null;
            }
            //if (TwitchPubSub != null)
            //{
            //    TwitchPubSub.Disconnect();
            //    TwitchPubSub = null;
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
                Console.WriteLine("Not connected to Twitch!");
                return;
            }
            TwitchClient.SendMessage(Channel, message);
        }

        private void TwitchClient_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine("Connected to Twitch using username: " + e.BotUsername);
        }

        private void TwitchClient_OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        {
            if (DebugLogs)
            {
                Console.WriteLine($"[Twitch] {e.DateTime}: {e.BotUsername} - {e.Data}");
            }
        }

    }
}
