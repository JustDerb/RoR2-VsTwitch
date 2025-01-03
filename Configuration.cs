using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine.Events;

namespace VsTwitch
{
    public class Configuration
	{
        // Twitch
        public ConfigEntry<bool> TwitchDebugLogs { get; }
        public ConfigEntry<bool> EnableItemVoting { get; }
        public ConfigEntry<int> VoteDurationSec { get; }
        public ConfigEntry<VoteStrategies> VoteStrategy { get; }

        public enum VoteStrategies
        {
            MaxVote,
            MaxVoteRandomTie,
            Percentile
        }

        public ConfigEntry<bool> EnableBitEvents { get; }
        public ConfigEntry<int> BitsThreshold { get; }
        public ConfigEntry<int> CurrentBits { get; }
        public ConfigEntry<bool> PublishToChat { get; }

        // Tiltify
        public ConfigEntry<string> TiltifyCampaignId { get; }

        // Event
        public ConfigEntry<float> BitStormWeight { get; }
        public ConfigEntry<float> BountyWeight { get; }
        public ConfigEntry<float> ShrineOfOrderWeight { get; }
        public ConfigEntry<float> ShrineOfTheMountainWeight { get; }
        public ConfigEntry<float> TitanWeight { get; }
        public ConfigEntry<float> LunarWispWeight { get; }
        public ConfigEntry<float> MithrixWeight { get; }
        public ConfigEntry<float> ElderLemurianWeight { get; }

        // Channel Points
        public ConfigEntry<bool> ChannelPointsEnable { get; }
        public ConfigEntry<string> ChannelPointsAllyBeetle { get; }
        public ConfigEntry<string> ChannelPointsAllyLemurian { get; }
        public ConfigEntry<string> ChannelPointsAllyElderLemurian { get; }
        public ConfigEntry<string> ChannelPointsRustedKey { get; }
        public ConfigEntry<string> ChannelPointsBitStorm { get; }
        public ConfigEntry<string> ChannelPointsBounty { get; }
        public ConfigEntry<string> ChannelPointsShrineOfOrder { get; }
        public ConfigEntry<string> ChannelPointsShrineOfTheMountain { get; }
        public ConfigEntry<string> ChannelPointsTitan { get; }
        public ConfigEntry<string> ChannelPointsLunarWisp { get; }
        public ConfigEntry<string> ChannelPointsMithrix { get; }
        public ConfigEntry<string> ChannelPointsElderLemurian { get; }

        // UI
        public ConfigEntry<bool> SimpleUI { get; }

        // Behaviour
        public ConfigEntry<bool> EnableChoosingLunarItems { get; }
        public ConfigEntry<bool> ForceUniqueRolls { get; }

        public Configuration(BaseUnityPlugin plugin, UnityAction reloadChannelPoints)
		{
            // These are old settings; be sure to remove them to curb confusion
            EnsureConfigsRemoved(plugin.Config, new List<ConfigDefinition>() {
                new ConfigDefinition("Twitch", "Channel"),
                new ConfigDefinition("Twitch", "ClientID"),
                new ConfigDefinition("Twitch", "Username"),
                new ConfigDefinition("Twitch", "ImplicitOAuth"),
                new ConfigDefinition("Language", "EnableLanguageEdits"),
            });

            // Twitch
            TwitchDebugLogs = plugin.Config.Bind("Twitch", "DebugLogs", false, "Enable debug logging for Twitch - will spam to the console!");
            EnableItemVoting = plugin.Config.Bind("Twitch", "EnableItemVoting", true, "Enable item voting on Twitch.");
            VoteDurationSec = plugin.Config.Bind("Twitch", "VoteDurationdSec", 10, "How long to allow twitch voting.");
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

            if (ModCompatibility.RiskOfOptions.Enabled)
            {
                ModCompatibility.RiskOfOptions.ApplyRiskOfOptions(this, reloadChannelPoints);
            }
        }

        private static void EnsureConfigsRemoved(ConfigFile config, List<ConfigDefinition> configDefinitions)
        {
            foreach (var configDefinition in configDefinitions)
            {
                // Need to do this to remove it from the internal "orphaned setting" map
                config.Bind(configDefinition, "");
                config.Remove(configDefinition);
            }
            config.Save();
        }
    }
}
