using TwitchSDK;
using TwitchSDK.Interop;
using System;
using Diag = System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.Net.Http.Headers;

namespace VsTwitch.Twitch
{
    #region Twitch API Singleton
    class UnityTwitch : TwitchSDKApi
    {
        UnityPAL PAL;
        public UnityTwitch(string clientId, bool useESProxy) : base(clientId, useESProxy)
        {
        }

        public void InitializeInternally()
        {
            PAL.Start();
        }

        protected override PlatformAbstractionLayer CreatePAL()
        {
            // We need to save this in a variable, so we can call InitializeInternally later.
            return (PAL = new UnityPAL());
        }

        class UnityPAL : ManagedPAL
        {
            TaskCompletionSource<string> FileIOBasePathTCS = new TaskCompletionSource<string>();

            static UnityPAL()
            {
                TaskScheduler.UnobservedTaskException += (a, exc) =>
                {
                    if (exc.Exception.InnerException.GetType() == typeof(CoreLibraryException))
                    {
                        Debug.LogWarning("Unhandled Twitch Exception: " + exc.Exception.InnerException);
                    }
                };
            }

            public void Start()
            {
                FileIOBasePathTCS.SetResult(Application.persistentDataPath);
            }

            protected override Task Log(LogRequest req)
            {
                switch (req.Level)
                {
                    case LogLevel.Debug:
                        // don't show
                        break;
                    case LogLevel.Warning:
                        Debug.LogWarning(req.Message);
                        break;
                    case LogLevel.Error:
                        Debug.LogError(req.Message);
                        break;
                    default:
                        Debug.Log(req.Message);
                        break;
                }
                return Task.CompletedTask;
            }

            // replace FALSE with TRUE to debug / inspect the HTTP requests of the plugin.
#if FALSE
        protected override async Task<WebRequestResult> WebRequest(WebRequestRequest request)
        {
            var isAuthRequest = request.Uri.IndexOf("https://id.twitch.tv/") == 0;
            var displayUri = request.Uri;

            if (isAuthRequest && request.Uri.IndexOf("?") != -1)
                displayUri = request.Uri.Substring(0, request.Uri.IndexOf("?")) + "?redacted.";

            Debug.Log("Getting " + displayUri);
            var res = await base.WebRequest(request);
            Debug.Log("Response to " + displayUri + " is of code " + res.HttpStatus + " and has the body: \r\n" + (isAuthRequest ? "redacted" : res.ResponseBody));
            return res;
        }
#endif

            protected override Task<string> GetFileIOBasePath(CancellationToken _)
            {
                return FileIOBasePathTCS.Task;
            }

            protected override string HttpUserAgent => "Twitch-Route-66-Unity";
        }
    }

    public class Twitch : MonoBehaviour
    {
        private static object Lock = new object();

        private static Twitch _Twitch;

        public static TwitchSDKApi API
        {
            get
            {
                lock (Lock)
                {
                    if (_Twitch != null && _Twitch.Instance != null)
                        return _Twitch.Instance;

                    try
                    {
                        _Twitch = FindObjectOfType<Twitch>();
                    }
                    catch (UnityException e) when (e.HResult == -2147467261)
                    {
                        throw new Exception("The Twitch API can only be initialized on the main thread. Make sure the first invocation of Twitch.API happens in the Unity Main thread (e.g. the Start or Update method, and not a constructor)");
                    }

                    if (_Twitch != null && _Twitch.Instance != null)
                        Destroy(_Twitch.gameObject);

                    if (_Twitch == null)
                    {
                        var singletonObject = new GameObject();
                        _Twitch = singletonObject.AddComponent<Twitch>();
                        _Twitch.CreateInstance();
                        singletonObject.name = "TwitchApi (Singleton)";

                        // Make instance persistent.
                        DontDestroyOnLoad(singletonObject);
                    }

                    return _Twitch.Instance;
                }
            }
        }
        private TwitchSDKApi Instance;

        public Twitch()
        {
        }

        private void CreateInstance()
        {
            var settings = TwitchSDKSettings.Instance;

            if (settings.ClientId == TwitchSDKSettings.InitialClientId)
            {
                Debug.LogError("Twitch: No OAuth ClientId set. Please open the Twitch settings at Twitch->Edit Settings.");
            }

            Instance = new UnityTwitch(settings.ClientId, settings.UseEventSubProxy);
            ((UnityTwitch)Instance).InitializeInternally();
        }


        private void OnApplicationQuit()
        {
            if (Instance != null)
            {
                Debug.Log("OnApplicationQuit Twitch API");
                Instance.Dispose();
                Instance = null;
            }
        }

        private void OnDestroy()
        {
            if (Instance != null)
            {
                Debug.Log("OnDestroy Twitch API");
                Instance.Dispose();
                Instance = null;
            }
        }
    }

    #endregion
}