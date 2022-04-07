using System;

namespace VsTwitch
{
    class OnDonationArgs : EventArgs
    {
        public double Amount;
        public string Name;
        public string Comment;
    }
}
