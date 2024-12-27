using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace VsTwitch.Twitch.Auth
{
    internal class WebServer : IDisposable
    {
        private readonly static string MainBodyResponse = 
            "  <h1 id=\"title\">Authorizing...</h1>\n" +
            "  <p id=\"message\">If this page does not change in a couple seconds check your AdBlocker settings (this page needs to send details to this website, via localhost, and could be blocked).</p>\n" +
            "  <script>\n" +
            "    // Extract the hash and resubmit it so the app can capture it\n" +
            "    fetch(window.location.toString(), {\n" +
            "      method: 'POST',\n" +
            "      headers: {\n" +
            "        'hash': window.location.hash\n" +
            "      },\n" +
            "    }).then((response) => {\n" +
            "      if (!response.ok) {\n" +
            "        document.getElementById('title').innerText = 'Error!';\n" +
            "        document.getElementById('message').innerText = 'There was an unexpected error!';\n" +
            "      } else {\n" +
            "        window.location.hash = '';\n" +
            "        document.getElementById('title').innerText = 'Success!';\n" +
            "        document.getElementById('message').innerText = 'Vs Twitch integration authorized! You can now close this tab and return to your game.';\n" +
            "      }\n" +
            "    });\n" +
            "  </script>";
        private readonly static string ResponseTemplate =
            "<!DOCTYPE html>\n" +
            "<html lang=\"en\">\n" +
            "<head>\n" +
            "  <title>Vs Twitch - Risk of Rain 2</title>\n" +
            "  <meta charset=\"UTF-8\">\n" +
            "</head>\n" +
            "<body>\n" +
            "  {{content}}\n" +
            "</body>\n" +
            "</html>";

        /// <summary>
        /// Minimum scope needed to run the mod
        /// </summary>
        private readonly static ReadOnlyCollection<string> MinimumScopes = new List<string>()
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

        public readonly static string TwitchClientId = "ms931m917okbj4hu8l230hejiagie0";
        private readonly static string TwitchRedirectUri = "http://localhost:9876/auth/redirect/";

        private HttpListener listener;

        public WebServer()
        {
            listener = new HttpListener();
            listener.Prefixes.Add(TwitchRedirectUri);
        }

        public void Listen()
        {
            listener.Start();
        }

        public async Task<Models.Authorization> OnRequest()
        {
            while (listener.IsListening)
            {
                Log.Info($"Waiting for callback");
                var ctx = await listener.GetContextAsync().ConfigureAwait(false);
                Log.Info($"Got request!");
                var req = ctx.Request;
                var res = ctx.Response;

                string responseContent = ResponseTemplate;

                if (req.QueryString.AllKeys.Any("error".Contains))
                {
                    Log.Info($"Got error from Twitch");
                    WriteResponse(res, responseContent.Replace("{{content}}",
                        "<h1>Error!</h1>\n" +
                        $"<p>The VS Twitch integration authorization encountered a problem: {HttpUtility.HtmlEncode(req.QueryString["error_description"])}<p>\n"
                    ), 400);
                    throw new InvalidOperationException($"error authorizing app: {req.QueryString["error_description"]}");
                }

                string hashHeader = req.Headers["hash"];
                if (hashHeader == null) {
                    // First page load, coming from Twitch
                    Log.Info($"First page!");
                    WriteResponse(res, responseContent.Replace("{{content}}", MainBodyResponse));
                    continue;
                }

                // If we've made it here, our own Javascript has sent the auth details
                // Make sure we've POST'd it
                if (!string.Equals(req.HttpMethod, "post", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Info($"Second load not a POST!");
                    WriteResponse(res, "", 405);
                    continue;
                }

                // Try to extract what we need
                if (hashHeader.StartsWith("#"))
                {
                    hashHeader = hashHeader.Substring(1);
                }
                var hashMap = HttpUtility.ParseQueryString(hashHeader);
                Log.Warning(hashHeader);
                string accessToken = hashMap["access_token"];
                if (string.IsNullOrEmpty(accessToken))
                {
                    Log.Info($"access_token not found!");
                    WriteResponse(res, "{\"error\":\"No access_token found in hash header\"}", 400, "application/json");
                    continue;
                }

                Log.Info($"GOT IT!");
                WriteResponse(res, "{}", 200, "application/json");
                return new Models.Authorization(accessToken);
            }

            return null;
        }

        private static void WriteResponse(HttpListenerResponse resp, string content, int statusCode = 200, string contentType = "text/html")
        {
            resp.StatusCode = statusCode;
            resp.ContentType = contentType;
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            resp.ContentLength64 = bytes.LongLength;
            resp.OutputStream.Write(bytes, 0, bytes.Length);
            resp.Close();
        }

        public void Dispose()
        {
            listener.Close();
        }

        /// <summary>
        /// Get URL for the Twitch Implicit grant flow.
        /// </summary>
        /// <returns>URL for user to visit to authorize the app</returns>
        /// <seealso cref="https://dev.twitch.tv/docs/authentication/getting-tokens-oauth/#implicit-grant-flow"/>
        public string GetAuthorizationTokenUrl(string state)
        {
            var scopesStr = string.Join('+', MinimumScopes);

            return "https://id.twitch.tv/oauth2/authorize" +
                   $"?client_id={TwitchClientId}" +
                   $"&redirect_uri={HttpUtility.UrlEncode(TwitchRedirectUri)}" +
                   "&response_type=token" +
                   $"&scope={scopesStr}" +
                   $"&state={HttpUtility.UrlEncode(state)}";
        }
    }
}
