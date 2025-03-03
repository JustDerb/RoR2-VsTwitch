using System;
using System.Collections.Generic;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;

namespace VsTwitch
{
    class ChannelPointsManager
    {
        private readonly Dictionary<string, Action<ChannelPointsManager, ChannelPointsCustomRewardRedemption>> channelEvents;

        public ChannelPointsManager()
        {
            channelEvents = new Dictionary<string, Action<ChannelPointsManager, ChannelPointsCustomRewardRedemption>>();
        }

        public bool RegisterEvent(string eventName, Action<ChannelPointsManager, ChannelPointsCustomRewardRedemption> e) {
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

        public bool TriggerEvent(ChannelPointsCustomRewardRedemption e)
        {
            string title = e.Reward.Title;
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
