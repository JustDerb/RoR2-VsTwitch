using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.Options;
using RiskOfOptions.OptionConfigs;
using System.Runtime.CompilerServices;
using UnityEngine.Events;

namespace VsTwitch.ModCompatibility
{
    internal class RiskOfOptions
    {
        public const string ModGUID = "com.rune580.riskofoptions";

        private static bool? enabled;

        public static bool Enabled
        {
            get
            {
                if (enabled == null)
                {
                    enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(ModGUID);
                }
                return (bool)enabled;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void ApplyRiskOfOptions(Configuration config, UnityAction reloadChannelPoints)
        {
            try
            {
                ModSettingsManager.SetModDescription("Fight Twitch chat. Item voting, bit events, channel points integration, and more!");
                // Twitch
                ModSettingsManager.AddOption(new StringInputFieldOption(config.TwitchChannel,
                    new InputFieldConfig() { restartRequired = true }));
                ModSettingsManager.AddOption(new StringInputFieldOption(config.TwitchUsername,
                    new InputFieldConfig() { restartRequired = true }));
                ModSettingsManager.AddOption(new CheckBoxOption(config.TwitchDebugLogs));
                ModSettingsManager.AddOption(new CheckBoxOption(config.EnableItemVoting));
                ModSettingsManager.AddOption(new IntSliderOption(config.VoteDurationSec,
                    new IntSliderConfig() { min = 1, max = 120, checkIfDisabled = () => !config.EnableItemVoting.Value }));
                ModSettingsManager.AddOption(new ChoiceOption(config.VoteStrategy,
                    new ChoiceConfig() { restartRequired = true, checkIfDisabled = () => !config.EnableItemVoting.Value }));
                ModSettingsManager.AddOption(new CheckBoxOption(config.PublishToChat,
                    new CheckBoxConfig() { checkIfDisabled = () => !config.EnableItemVoting.Value }));
                ModSettingsManager.AddOption(new CheckBoxOption(config.EnableBitEvents));
                ModSettingsManager.AddOption(new IntSliderOption(config.BitsThreshold,
                    // $1 to $1000
                    new IntSliderConfig() { min = 100, max = 100 * 1000 }));

                // Tiltify
                ModSettingsManager.AddOption(new StringInputFieldOption(config.TiltifyCampaignId,
                    new InputFieldConfig() { restartRequired = true }));

                // Event
                foreach (var bitEvent in new List<ConfigEntry<float>>() {
                    config.BitStormWeight,
                    config.BountyWeight,
                    config.ShrineOfOrderWeight,
                    config.ShrineOfTheMountainWeight,
                    config.TitanWeight,
                    config.LunarWispWeight,
                    config.MithrixWeight,
                    config.ElderLemurianWeight,
                })
                {
                    ModSettingsManager.AddOption(new StepSliderOption(bitEvent,
                        new StepSliderConfig() { min = 0f, max = 10f, increment = 1f }));
                }

                // Channel Points
                ModSettingsManager.AddOption(new CheckBoxOption(config.ChannelPointsEnable));
                ModSettingsManager.AddOption(new GenericButtonOption("Re-apply Channel Points Config", "ChannelPoints",
                    "Reload Channel Points settings in the game. You MUST click this button if you've changed any entries on this Channel Points page for it to take effect!",
                    "Apply", reloadChannelPoints));
                foreach (var channelPointsAlly in new List<ConfigEntry<string>>() {
                    config.ChannelPointsAllyBeetle,
                    config.ChannelPointsAllyLemurian,
                    config.ChannelPointsAllyElderLemurian,
                    config.ChannelPointsRustedKey,
                    config.ChannelPointsBitStorm,
                    config.ChannelPointsBounty,
                    config.ChannelPointsShrineOfOrder,
                    config.ChannelPointsShrineOfTheMountain,
                    config.ChannelPointsTitan,
                    config.ChannelPointsLunarWisp,
                    config.ChannelPointsMithrix,
                    config.ChannelPointsElderLemurian,
                })
                {
                    ModSettingsManager.AddOption(new StringInputFieldOption(channelPointsAlly,
                        new InputFieldConfig() { checkIfDisabled = () => !config.ChannelPointsEnable.Value }));
                }

                // UI
                ModSettingsManager.AddOption(new CheckBoxOption(config.SimpleUI));

                // Behaviour
                ModSettingsManager.AddOption(new CheckBoxOption(config.EnableChoosingLunarItems));
                ModSettingsManager.AddOption(new CheckBoxOption(config.ForceUniqueRolls));

                //Language
                ModSettingsManager.AddOption(new CheckBoxOption(config.EnableLanguageEdits,
                    new CheckBoxConfig() { restartRequired = true }));
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }
    }
}
