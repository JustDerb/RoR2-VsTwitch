using System;

namespace VsTwitch
{
    class TiltifyManager
    {
        private Tiltify.TiltifyWebSocket tiltifyWebsocket;
        private int campaignId;

        //private long lastDonationTime;

        public event EventHandler<OnDonationArgs> OnDonationReceived;
        public event EventHandler<EventArgs> OnConnected;
        public event EventHandler<EventArgs> OnDisconnected;

        public TiltifyManager()
        {
        }

        public void Connect(int campaignId)
        {
            Disconnect();

            if (campaignId <= 0)
            {
                throw new ArgumentException("Tiltify campaign ID must be specified!", "campaignId");
            }
            this.campaignId = campaignId;
            //this.lastDonationTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            try
            {
                tiltifyWebsocket = new Tiltify.TiltifyWebSocket();
                tiltifyWebsocket.OnLog += TiltifyWebsocket_OnLog;
                tiltifyWebsocket.OnTiltifyServiceConnected += TiltifyWebsocket_OnTiltifyServiceConnected;
                tiltifyWebsocket.OnCampaignDonation += TiltifyWebsocket_OnCampaignDonation;

                tiltifyWebsocket.Connect();

                tiltifyWebsocket.OnTiltifyServiceClosed += TiltifyWebsocket_OnTiltifyServiceClosed;
            }
            catch (Exception e)
            {
                tiltifyWebsocket = null;
                throw e;
            }
        }

        private void TiltifyWebsocket_OnLog(object sender, Tiltify.Events.OnLogArgs e)
        {
            Console.WriteLine($"[Tiltify Websocket] {e.Data}");
        }

        private void TiltifyWebsocket_OnCampaignDonation(object sender, Tiltify.Events.OnCampaignDonationArgs e)
        {
            OnDonationReceived.Invoke(this, new OnDonationArgs()
            {
                Amount = e.Donation.Amount,
                Name = e.Donation.Name,
                Comment = e.Donation.Comment,
            });
        }

        private void TiltifyWebsocket_OnTiltifyServiceClosed(object sender, EventArgs e)
        {
            OnDisconnected.Invoke(this, e);
        }

        private void TiltifyWebsocket_OnTiltifyServiceConnected(object sender, EventArgs e)
        {
            if (campaignId > 0)
            {
                tiltifyWebsocket.ListenToCampaignDonations(campaignId.ToString());
                tiltifyWebsocket.SendTopics();
            }
            OnConnected.Invoke(this, e);
        }

        public void Disconnect()
        {
            if (tiltifyWebsocket != null && tiltifyWebsocket.IsConnected)
            {
                tiltifyWebsocket.Disconnect();
            }
            tiltifyWebsocket = null;
            campaignId = 0;
            OnDisconnected.Invoke(this, EventArgs.Empty);
        }

        public bool IsConnected()
        {
            return tiltifyWebsocket != null && tiltifyWebsocket.IsConnected;
        }

        //public void CheckForDonations(int forceCampaignId = 0)
        //{
        //    if (!IsConnected())
        //    {
        //        return;
        //    }

        //    int campaignId = forceCampaignId == 0 ? this.campaignId : forceCampaignId;

        //    Task <GetCampaignDonationsResponse> response = this.tiltify.Campaigns.GetCampaignDonations(campaignId);
        //    response.ContinueWith((donations) => {
        //        // Ensure we are processing the older donations first because of how we are tracking donations
        //        // so we don't repeat them.
        //        Array.Sort(donations.Result.Data, new DonationInformationReverser());

        //        foreach (DonationInformation donation in donations.Result.Data)
        //        {
        //            // This assumes that the order is most recent to least recent
        //            if (donation.CompletedAt > this.lastDonationTime)
        //            {
        //                this.lastDonationTime = donation.CompletedAt;
        //                this.OnDonationReceived.Invoke(this, new OnDonationArgs()
        //                {
        //                    Amount = donation.Amount,
        //                    Name = donation.Name,
        //                    Comment = donation.Comment,
        //                });
        //            }
        //        }
        //    });
        //}

        //private class DonationInformationReverser : IComparer<DonationInformation>
        //{
        //    public int Compare(DonationInformation x, DonationInformation y)
        //    {
        //        return Convert.ToInt32(x.CompletedAt - y.CompletedAt);
        //    }
        //}
    }
}
