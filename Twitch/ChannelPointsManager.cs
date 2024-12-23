using System;
using System.Collections.Generic;
using TwitchLib.PubSub.Events;

namespace VsTwitch
{
    class ChannelPointsManager
    {
        private readonly Dictionary<string, Action<ChannelPointsManager, OnChannelPointsRewardRedeemedArgs>> channelEvents;

        public ChannelPointsManager()
        {
            channelEvents = new Dictionary<string, Action<ChannelPointsManager, OnChannelPointsRewardRedeemedArgs>>();
        }

        public bool RegisterEvent(string eventName, Action<ChannelPointsManager, OnChannelPointsRewardRedeemedArgs> e) {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return false;
            }

            channelEvents[eventName] = e;
            return true;
        }

        public bool UnregisterEvent(string eventName)
        {
            if (!channelEvents.ContainsKey(eventName))
            {
                return false;
            }
            
            channelEvents.Remove(eventName);
            return true;
        }

        public bool TriggerEvent(OnChannelPointsRewardRedeemedArgs e)
        {
            string title = e.RewardRedeemed.Redemption.Reward.Title;
            if (!channelEvents.ContainsKey(title))
            {
                return false;
            }

            try
            {
                channelEvents[title](this, e);
                return true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                return false;
            }
        }
    }
}
