using System;
using TwitchSDK;
using TwitchSDK.Interop;
using UnityEngine;

namespace VsTwitch.Twitch
{
    internal class TwitchManager : MonoBehaviour
    {
        public event EventHandler<BitsEvent>? OnBitsEvent;
        public event EventHandler<RewardEvent>? OnRewardEvent;

        private GameTask<EventStream<CustomRewardEvent>>? CustomRewardEvents;
        private GameTask<EventStream<ChannelCheerEvent>>? ChannelCheerEvents;

        public void Start()
        {
            CustomRewardEvents = Twitch.API.SubscribeToCustomRewardEvents();
            ChannelCheerEvents = Twitch.API.SubscribeToChannelCheerEvents();
        }

        public void Update()
        {
            if (CustomRewardEvents != null)
            {
                CustomRewardEvent CurRewardEvent;
                CustomRewardEvents.MaybeResult.TryGetNextEvent(out CurRewardEvent);
                if (CurRewardEvent != null)
                {
                    // Do something
                    Log.Warning($"{CurRewardEvent.RedeemerName} has brought {CurRewardEvent.CustomRewardTitle} for {CurRewardEvent.CustomRewardCost}!");
                    OnRewardEvent?.Invoke(this, new RewardEvent()
                    {
                        Bits = 0,
                    });
                }
            }

            if (ChannelCheerEvents != null)
            {
                ChannelCheerEvent CurCheerEvent;
                ChannelCheerEvents.MaybeResult.TryGetNextEvent(out CurCheerEvent);
                if (CurCheerEvent != null)
                {
                    // Do something
                    Log.Warning($"{CurCheerEvent.UserDisplayName} has cheered {CurCheerEvent.Bits}: {CurCheerEvent.Message}!");
                    OnBitsEvent?.Invoke(this, new BitsEvent()
                    {
                        Bits = CurCheerEvent.Bits,
                    });
                }
            }
        }

        private void OnDestroy()
        {

        }
    }
}
