using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TwitchLib.Api;
using UnityEngine;

namespace VsTwitch.Twitch.Auth
{
    internal class AuthManager
    {
        private readonly string AuthFilePath;

        public AuthManager(string authFilePath, ILoggerFactory loggerFactory)
        {
            AuthFilePath = authFilePath;
            TwitchAPI = new TwitchAPI(loggerFactory);
            TwitchAPI.Settings.ClientId = WebServer.TwitchClientId;
            TwitchAPI.Settings.SkipAutoServerTokenGeneration = true;

            // TODO: Pull access token from authFilePath
        }

        public TwitchAPI TwitchAPI { get; private set; }

        public async Task<bool> IsAuthed(bool useApiCallToCheck = false)
        {
            Log.Info($"IsAuthed({useApiCallToCheck}) 1");
            bool authed = !string.IsNullOrEmpty(TwitchAPI.Settings.AccessToken);
            if (authed && useApiCallToCheck)
            {
                Log.Info($"IsAuthed({useApiCallToCheck}) 2");
                var resp = await TwitchAPI.Auth.ValidateAccessTokenAsync().ConfigureAwait(false);
                if (resp == null)
                {
                    Log.Info($"IsAuthed({useApiCallToCheck}) 3");
                    return false;
                }
                // Doesn't expire in less than 30 minutes
                authed = resp.ExpiresIn >= 30 * 60;
            }

            Log.Info($"IsAuthed({useApiCallToCheck}) 4");
            return authed;
        }

        public async Task<ReadOnlyCollection<string>> GetAuthorizedScopes()
        {
            if (!await IsAuthed())
            {
                return new List<string>().AsReadOnly();
            }

            var resp = await TwitchAPI.Auth.ValidateAccessTokenAsync();
            if (resp == null)
            {
                return new List<string>().AsReadOnly();
            }
            return resp.Scopes.AsReadOnly();
        }

        public async Task MaybeAuthUser()
        {
            Log.Info("MaybeAuthUser() 1");
            if (await IsAuthed(true))
            {
                Log.Info("MaybeAuthUser() 1a");
                return;
            }

            Log.Info("MaybeAuthUser() 2");
            WebServer webServer = new WebServer();
            webServer.Listen();

            Log.Info("MaybeAuthUser() 3");
            string authTokenUrl = webServer.GetAuthorizationTokenUrl(""); // FIXME
            Application.OpenURL(authTokenUrl);

            Log.Info("MaybeAuthUser() 4");
            Models.Authorization auth = await webServer.OnRequest().ConfigureAwait(false);
            if (auth == null)
            {
                Log.Info("MaybeAuthUser() 4a");
                throw new InvalidOperationException("WebServer didn't return an auth object");
            }

            Log.Info("MaybeAuthUser() 5");
            TwitchAPI.Settings.AccessToken = auth.AccessToken;
        }
    }
}
