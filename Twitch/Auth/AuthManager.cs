using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private readonly static string TWITCH_CLIENT_ID = "ms931m917okbj4hu8l230hejiagie0";
        private readonly static string DATA_MANAGER_OAUTH_KEY = "twitchOAuth";

        private readonly DataManager dataManager;

        private DateTimeOffset tokenExpiry = DateTimeOffset.MinValue;

        public TwitchAPI TwitchAPI { get; }
        public TwitchClient TwitchClient { get; }
        public string TwitchUsername { get; private set; }
        public string TwitchChannelId { get; private set; }

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
            && tokenExpiry > DateTimeOffset.UtcNow;

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

            string authTokenUrl = webServer.GetAuthorizationTokenUrl(""); // FIXME
            Application.OpenURL(authTokenUrl);

            Log.Info("Waiting for Twitch Authorization token...");
            Models.Authorization auth = await webServer.GetAuthorization().ConfigureAwait(false);
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
            tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(resp.ExpiresIn);

            ConnectionCredentials credentials = new ConnectionCredentials(resp.Login, oauth);
            TwitchClient.Initialize(credentials, resp.Login);

            return true;
        }
    }
}
