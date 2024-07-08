using BepInEx;
using BepInEx.Configuration;
using UnityEngine.Events;

namespace VsTwitch
{
	public class Configuration
	{
        // Twitch
        public ConfigEntry<string> TwitchChannel { get; set; }
        public ConfigEntry<string> TwitchClientID { get; set; }
        public ConfigEntry<string> TwitchUsername { get; set; }
        public ConfigEntry<string> TwitchOAuth { get; set; }
        public ConfigEntry<bool> TwitchDebugLogs { get; set; }
        public ConfigEntry<bool> EnableItemVoting { get; set; }
        public ConfigEntry<int> VoteDurationSec { get; set; }
        public ConfigEntry<VoteStrategies> VoteStrategy { get; set; }

        public enum VoteStrategies
        {
            MaxVote,
            MaxVoteRandomTie,
            Percentile
        }

        public ConfigEntry<bool> EnableBitEvents { get; set; }
        public ConfigEntry<int> BitsThreshold { get; set; }
        public ConfigEntry<int> CurrentBits { get; set; }
        public ConfigEntry<bool> PublishToChat { get; set; }

        // Tiltify
        public ConfigEntry<string> TiltifyCampaignId { get; set; }

        // Event
        public ConfigEntry<float> BitStormWeight { get; set; }
        public ConfigEntry<float> BountyWeight { get; set; }
        public ConfigEntry<float> ShrineOfOrderWeight { get; set; }
        public ConfigEntry<float> ShrineOfTheMountainWeight { get; set; }
        public ConfigEntry<float> TitanWeight { get; set; }
        public ConfigEntry<float> LunarWispWeight { get; set; }
        public ConfigEntry<float> MithrixWeight { get; set; }
        public ConfigEntry<float> ElderLemurianWeight { get; set; }

        // Channel Points
        public ConfigEntry<bool> ChannelPointsEnable { get; set; }
        public ConfigEntry<string> ChannelPointsAllyBeetle { get; set; }
        public ConfigEntry<string> ChannelPointsAllyLemurian { get; set; }
        public ConfigEntry<string> ChannelPointsAllyElderLemurian { get; set; }
        public ConfigEntry<string> ChannelPointsRustedKey { get; set; }
        public ConfigEntry<string> ChannelPointsBitStorm { get; set; }
        public ConfigEntry<string> ChannelPointsBounty { get; set; }
        public ConfigEntry<string> ChannelPointsShrineOfOrder { get; set; }
        public ConfigEntry<string> ChannelPointsShrineOfTheMountain { get; set; }
        public ConfigEntry<string> ChannelPointsTitan { get; set; }
        public ConfigEntry<string> ChannelPointsLunarWisp { get; set; }
        public ConfigEntry<string> ChannelPointsMithrix { get; set; }
        public ConfigEntry<string> ChannelPointsElderLemurian { get; set; }

        // UI
        public ConfigEntry<bool> SimpleUI { get; set; }

        // Behaviour
        public ConfigEntry<bool> EnableChoosingLunarItems { get; set; }
        public ConfigEntry<bool> ForceUniqueRolls { get; set; }

        //Language
        public ConfigEntry<bool> EnableLanguageEdits { get; set; }

        public Configuration(BaseUnityPlugin plugin, UnityAction reloadChannelPoints)
		{
            // Twitch
            TwitchChannel = plugin.Config.Bind("Twitch", "Channel", "", "Your Twitch channel name. The channel to monitor Twitch chat.");
            TwitchClientID = plugin.Config.Bind("Twitch", "ClientID", "q6batx0epp608isickayubi39itsckt", "Client ID used to get ImplicitOAuth value");
            TwitchUsername = plugin.Config.Bind("Twitch", "Username", "", "Your Twitch username. The username to use when calling Twitch APIs. If you aren't using a secondary account, this should be the same as 'Channel'.");
            TwitchOAuth = plugin.Config.Bind("Twitch", "ImplicitOAuth", "", "Implicit OAuth code (this is not your password - it's a generated password!). See the README/Mod Description in the thunderstore to see how to get it.");
            TwitchDebugLogs = plugin.Config.Bind("Twitch", "DebugLogs", false, "Enable debug logging for Twitch - will spam to the console!");
            EnableItemVoting = plugin.Config.Bind("Twitch", "EnableItemVoting", true, "Enable item voting on Twitch.");
            VoteDurationSec = plugin.Config.Bind("Twitch", "VoteDurationdSec", 20, "How long to allow twitch voting.");
            VoteStrategy = plugin.Config.Bind("Twitch", "VoteStrategy", VoteStrategies.MaxVote, "How to tabulate votes. One of: MaxVote, MaxVoteRandomTie, Percentile");
            EnableBitEvents = plugin.Config.Bind("Twitch", "EnableBitEvents", true, "Enable bit events from Twitch.");
            BitsThreshold = plugin.Config.Bind("Twitch", "BitsThreshold", 1500, "How many Bits are needed before something happens.");
            CurrentBits = plugin.Config.Bind("Twitch", "CurrentBits", 0, "(DO NOT EDIT) How many Bits have currently been donated.");
            PublishToChat = plugin.Config.Bind("Twitch", "PublishToChat", true, "Publish events (like voting) to Twitch chat.");
            // Tiltify
            TiltifyCampaignId = plugin.Config.Bind("Tiltify", "CampaignId", "", "Tiltify Campaign ID to track donations");
            // Event
            BitStormWeight = plugin.Config.Bind("Event", "BitStormWeight", 1f, "Weight for the bit storm bit event. Set to 0 to disable.");
            BountyWeight = plugin.Config.Bind("Event", "BountyWeight", 1f, "Weight for the doppleganger bit event. Set to 0 to disable.");
            ShrineOfOrderWeight = plugin.Config.Bind("Event", "ShrineOfOrderWeight", 1f, "Weight for the Shrine of Order bit event. Set to 0 to disable.");
            ShrineOfTheMountainWeight = plugin.Config.Bind("Event", "ShrineOfTheMountainWeight", 1f, "Weight for the Shrine of the Mountain bit event. Set to 0 to disable.");
            TitanWeight = plugin.Config.Bind("Event", "TitanWeight", 1f, "Weight for the Aurelionite bit event. Set to 0 to disable.");
            LunarWispWeight = plugin.Config.Bind("Event", "LunarWispWeight", 1f, "Weight for the Lunar Chimera (Wisp) bit event. Set to 0 to disable.");
            MithrixWeight = plugin.Config.Bind("Event", "MithrixWeight", 1f, "Weight for the Mithrix bit event. Set to 0 to disable.");
            ElderLemurianWeight = plugin.Config.Bind("Event", "ElderLemurianWeight", 1f, "Weight for the Elder Lemurian bit event. Set to 0 to disable.");
            // Channel Points
            ChannelPointsEnable = plugin.Config.Bind("ChannelPoints", "Enable", true, "Enable all Channel Point features.");
            ChannelPointsAllyBeetle = plugin.Config.Bind("ChannelPoints", "AllyBeetle", "", "(Case Sensitive!) Channel Points Title to spawn Ally Elite Beetle. Leave empty to disable.");
            ChannelPointsAllyLemurian = plugin.Config.Bind("ChannelPoints", "AllyLemurian", "", "(Case Sensitive!) Channel Points Title to spawn Ally Elite Lemurian. Leave empty to disable.");
            ChannelPointsAllyElderLemurian = plugin.Config.Bind("ChannelPoints", "AllyElderLemurian", "", "(Case Sensitive!) Channel Points Title to spawn Ally Elite Elder Lemurian. Leave empty to disable.");
            ChannelPointsRustedKey = plugin.Config.Bind("ChannelPoints", "RustedKey", "", "(Case Sensitive!) Channel Points Title to give everyone a Rusted Key. Leave empty to disable.");
            ChannelPointsBitStorm = plugin.Config.Bind("ChannelPoints", "BitStorm", "", "(Case Sensitive!) Channel Points Title for the bit storm bit event. Leave empty to disable.");
            ChannelPointsBounty = plugin.Config.Bind("ChannelPoints", "Bounty", "", "(Case Sensitive!) Channel Points Title for the doppleganger bit event. Leave empty to disable.");
            ChannelPointsShrineOfOrder = plugin.Config.Bind("ChannelPoints", "ShrineOfOrder", "", "(Case Sensitive!) Channel Points Title for the Shrine of Order bit event. Leave empty to disable.");
            ChannelPointsShrineOfTheMountain = plugin.Config.Bind("ChannelPoints", "ShrineOfTheMountain", "", "(Case Sensitive!) Channel Points Title for the Shrine of the Mountain bit event. Leave empty to disable.");
            ChannelPointsTitan = plugin.Config.Bind("ChannelPoints", "Titan", "", "(Case Sensitive!) Channel Points Title for the Aurelionite bit event. Leave empty to disable.");
            ChannelPointsLunarWisp = plugin.Config.Bind("ChannelPoints", "LunarWisp", "", "(Case Sensitive!) Channel Points Title for the Lunar Chimera (Wisp) bit event. Leave empty to disable.");
            ChannelPointsMithrix = plugin.Config.Bind("ChannelPoints", "Mithrix", "", "(Case Sensitive!) Channel Points Title for the Mithrix bit event. Leave empty to disable.");
            ChannelPointsElderLemurian = plugin.Config.Bind("ChannelPoints", "ElderLemurian", "", "(Case Sensitive!) Channel Points Title for the Elder Lemurian bit event. Leave empty to disable.");
            // UI
            SimpleUI = plugin.Config.Bind("UI", "SimpleUI", false, "Simplify the UI. Set to true if you are playing Multiplayer.");
            // Behaviour
            EnableChoosingLunarItems = plugin.Config.Bind("Behaviour", "EnableChoosingLunarItems", true, "Twitch Chat chooses items when opening lunar chests (pods)");
            ForceUniqueRolls = plugin.Config.Bind("Behaviour", "ForceUniqueRolls", false, "Ensure, when rolling for items, that they are always different. This doesn't affect multi-shops.");
            //Language
            EnableLanguageEdits = plugin.Config.Bind("Language", "EnableLanguageEdits", true, "Enable all Language Edits.");

            if (ModCompatibility.RiskOfOptions.Enabled)
            {
                ModCompatibility.RiskOfOptions.ApplyRiskOfOptions(this, reloadChannelPoints);
            }
        }
    }
}
