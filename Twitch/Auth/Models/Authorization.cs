namespace VsTwitch.Twitch.Auth.Models
{
    internal class Authorization
    {
        public string AccessToken { get; }

        public Authorization(string accessToken)
        {
            AccessToken = accessToken;
        }
    }
}
