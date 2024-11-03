using System;

namespace VsTwitch.Twitch
{
    class RewardEvent : EventArgs
    {
        public long Bits { get; set; }
    }
}
