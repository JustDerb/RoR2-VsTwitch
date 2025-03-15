using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Models;
using UnityEngine;
using VsTwitch.Data;

namespace VsTwitch.Twitch.Auth
{
    internal class AuthManager
    {
        private readonly static System.Random random = new System.Random();
        private readonly static string TWITCH_CLIENT_ID = "ms931m917okbj4hu8l230hejiagie0";
        private readonly static string DATA_MANAGER_OAUTH_KEY = "twitchOAuth";

        /// <summary>
        /// Minimum scope needed to run the mod
        /// </summary>
        private readonly static ReadOnlyCollection<string> TWITCH_SCOPES = new List<string>()
        {
            // View bits information for your channel.
            "bits:read",
            // View live Stream Chat and Rooms messages
            "chat:read",
            // Send live Stream Chat and Rooms messages
            "chat:edit",
            // Get a list of all subscribers to your channel and check if a user is subscribed to your channel
            "channel:read:subscriptions",
            // View your channel points custom reward redemptions
            "channel:read:redemptions",
            // View hype train data for a given channel.
            "channel:read:hype_train",
        }.AsReadOnly();

        private readonly DataManager dataManager;

        private DateTimeOffset tokenExpiry = DateTimeOffset.MinValue;

        public TwitchAPI TwitchAPI { get; }
        public TwitchClient TwitchClient { get; }
        public string TwitchUsername { get; private set; }
        public string TwitchChannelId { get; private set; }
        public ReadOnlyCollection<string> TwitchUserScopes { get; private set; }
        /// <summary>
        /// Set inside the Initialize() method to do a one-time compare of scopes from API and the TWITCH_SCOPES
        /// to eliminate a list comparison everytime IsAuthed() is called
        /// </summary>
        private bool TwitchUserScopesMatch = false;

        private AuthManager(DataManager dataManager, ILoggerFactory loggerFactory)
        {
            this.dataManager = dataManager;
            TwitchUsername = "";
            TwitchAPI = new TwitchAPI(loggerFactory);
            TwitchAPI.Settings.ClientId = TWITCH_CLIENT_ID;
            TwitchAPI.Settings.SkipAutoServerTokenGeneration = true;
            TwitchClient = new TwitchClient(null, ClientProtocol.WebSocket, null, loggerFactory);
        }

        public async static Task<AuthManager> Create(DataManager dataManager, ILoggerFactory loggerFactory)
        {
            AuthManager authManager = new AuthManager(dataManager, loggerFactory);

            if (dataManager.Contains(DATA_MANAGER_OAUTH_KEY))
            {
                Log.Debug("[Twitch Auth Manager] Reusing stored OAuth token...");
                bool inited = await authManager.Initialize(dataManager.Get<string>(DATA_MANAGER_OAUTH_KEY));

                if (inited)
                {
                    Log.Info("[Twitch Auth Manager] Stored OAuth token is valid!");
                }
                else
                {
                    Log.Warning("[Twitch Auth Manager] Stored OAuth token is invalid/expired. One will need to be requested.");
                }
            }
            else
            {
                Log.Warning("[Twitch Auth Manager] No stored OAuth token. One will need to be requested.");
            }

            return authManager;
        }

        public bool IsAuthed() => !string.IsNullOrEmpty(TwitchAPI.Settings.AccessToken)
            && !string.IsNullOrEmpty(TwitchUsername)
            && tokenExpiry > DateTimeOffset.UtcNow
            && TwitchUserScopesMatch;

        public async Task<ReadOnlyCollection<string>> GetAuthorizedScopes()
        {
            if (!IsAuthed())
            {
                return new List<string>().AsReadOnly();
            }

            var resp = await TwitchAPI.Auth.ValidateAccessTokenAsync().ConfigureAwait(false);
            if (resp == null)
            {
                return new List<string>().AsReadOnly();
            }
            return resp.Scopes.AsReadOnly();
        }

        public async Task MaybeAuthUser()
        {
            if (IsAuthed())
            {
                return;
            }

            using WebServer webServer = new WebServer(TWITCH_CLIENT_ID);
            webServer.Listen();

            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string randomState = new string(Enumerable.Repeat(chars, 32)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            string authTokenUrl = webServer.GetAuthorizationTokenUrl(randomState, TWITCH_SCOPES);
            Application.OpenURL(authTokenUrl);

            Log.Info("Waiting for Twitch Authorization token...");
            Models.Authorization auth = await webServer.GetAuthorization(randomState).ConfigureAwait(false);
            if (auth == null)
            {
                throw new InvalidOperationException("WebServer didn't return an auth object");
            }

            bool inited = await Initialize(auth.AccessToken).ConfigureAwait(false);
            if (!inited)
            {
                throw new InvalidOperationException("WebServer didn't return a valid access token");
            }
            dataManager.Set(DATA_MANAGER_OAUTH_KEY, auth.AccessToken);
        }

        private async Task<bool> Initialize(string oauth)
        {
            TwitchAPI.Settings.AccessToken = oauth;

            var resp = await TwitchAPI.Auth.ValidateAccessTokenAsync().ConfigureAwait(false);
            if (resp == null)
            {
                return false;
            }
            TwitchUsername = resp.Login;
            TwitchChannelId = resp.UserId;
            TwitchUserScopes = resp.Scopes.AsReadOnly();
            TwitchUserScopesMatch = Enumerable.SequenceEqual(TWITCH_SCOPES.OrderBy(e => e), TwitchUserScopes.OrderBy(e => e));
            tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(resp.ExpiresIn);

            ConnectionCredentials credentials = new ConnectionCredentials(resp.Login, oauth);
            TwitchClient.Initialize(credentials, resp.Login);

            return true;
        }
    }
}
