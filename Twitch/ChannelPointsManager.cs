using System;
using System.Collections.Generic;
using TwitchLib.PubSub.Events;

namespace VsTwitch
{
    class ChannelPointsManager
    {
        private Dictionary<string, Action<ChannelPointsManager, OnRewardRedeemedArgs>> channelEvents;

        public ChannelPointsManager()
        {
            channelEvents = new Dictionary<string, Action<ChannelPointsManager, OnRewardRedeemedArgs>>();
        }

        public bool RegisterEvent(string eventName, Action<ChannelPointsManager, OnRewardRedeemedArgs> e) {
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

        public bool TriggerEvent(OnRewardRedeemedArgs e)
        {
            if (!channelEvents.ContainsKey(e.RewardTitle))
            {
                return false;
            }

            try
            {
                channelEvents[e.RewardTitle](this, e);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }
    }
}
