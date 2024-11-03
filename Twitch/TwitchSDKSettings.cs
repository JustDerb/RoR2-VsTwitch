using UnityEngine;

namespace VsTwitch.Twitch
{
    class TwitchSDKSettings : ScriptableObject
    {
        public const string InitialClientId = "Go to dev.twitch.tv to get a client-id";

        public const string SettingsPath = "Project/TwitchSDKSettings";

        [SerializeField]
        public string ClientId = "";
        [SerializeField]
        public bool UseEventSubProxy = false;

        private static TwitchSDKSettings _Instance;

        public static TwitchSDKSettings Instance
        {
            get
            {
                _Instance = NullableInstance;
                if (_Instance == null)
                {
                    _Instance = CreateInstance<TwitchSDKSettings>();
                }
                return _Instance;
            }
        }
        public static TwitchSDKSettings NullableInstance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = Resources.Load(nameof(TwitchSDKSettings)) as TwitchSDKSettings;
                }
                return _Instance;
            }
        }
    }
}