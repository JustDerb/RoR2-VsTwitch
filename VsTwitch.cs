using BepInEx;
using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

// Allow scanning for ConCommand, and other stuff for Risk of Rain 2
[assembly: HG.Reflection.SearchableAttribute.OptIn]
namespace VsTwitch
{
    [BepInPlugin(GUID, ModName, Version)]
    public class VsTwitch : BaseUnityPlugin
    {
        private static readonly char[] SPACE = new char[] { ' ' };
        public const string GUID = "com.justinderby.vstwitch";
        public const string ModName = "VsTwitch";
        public const string Version = "1.0.12";

        // This is only used for ConCommands, since they need to be static...
        public static VsTwitch Instance;

        private TwitchManager twitchManager;
        private BitsManager bitsManager;
        private ChannelPointsManager channelPointsManager;
        private ItemRollerManager itemRollerManager;
        private LanguageOverride languageOverride;
        private EventDirector eventDirector;
        private EventFactory eventFactory;

        private TiltifyManager tiltifyManager;

        // Twitch
        public static ConfigEntry<string> TwitchChannel { get; set; }
        public static ConfigEntry<string> TwitchClientID { get; set; }
        public static ConfigEntry<string> TwitchUsername { get; set; }
        public static ConfigEntry<string> TwitchOAuth { get; set; }
        public static ConfigEntry<bool> TwitchDebugLogs { get; set; }
        public static ConfigEntry<bool> EnableItemVoting { get; set; }
        public static ConfigEntry<int> VoteDurationSec { get; set; }
        public static ConfigEntry<string> VoteStrategy { get; set; }
        public static ConfigEntry<bool> EnableBitEvents { get; set; }
        public static ConfigEntry<int> BitsThreshold { get; set; }
        public static ConfigEntry<int> CurrentBits { get; set; }
        public static ConfigEntry<bool> PublishToChat { get; set; }

        // Tiltify
        public static ConfigEntry<int> TiltifyCampaignId { get; set; }

        // Event
        public static ConfigEntry<float> BitStormWeight { get; set; }
        public static ConfigEntry<float> BountyWeight { get; set; }
        public static ConfigEntry<float> ShrineOfOrderWeight { get; set; }
        public static ConfigEntry<float> ShrineOfTheMountainWeight { get; set; }
        public static ConfigEntry<float> TitanWeight { get; set; }
        public static ConfigEntry<float> LunarWispWeight { get; set; }
        public static ConfigEntry<float> MithrixWeight { get; set; }
        public static ConfigEntry<float> ElderLemurianWeight { get; set; }

        // Channel Points
        public static ConfigEntry<bool> ChannelPointsEnable { get; set; }
        public static ConfigEntry<string> ChannelPointsAllyBeetle { get; set; }
        public static ConfigEntry<string> ChannelPointsAllyLemurian { get; set; }
        public static ConfigEntry<string> ChannelPointsAllyElderLemurian { get; set; }
        public static ConfigEntry<string> ChannelPointsRustedKey { get; set; }
        public static ConfigEntry<string> ChannelPointsBitStorm { get; set; }
        public static ConfigEntry<string> ChannelPointsBounty { get; set; }
        public static ConfigEntry<string> ChannelPointsShrineOfOrder { get; set; }
        public static ConfigEntry<string> ChannelPointsShrineOfTheMountain { get; set; }
        public static ConfigEntry<string> ChannelPointsTitan { get; set; }
        public static ConfigEntry<string> ChannelPointsLunarWisp { get; set; }
        public static ConfigEntry<string> ChannelPointsMithrix { get; set; }
        public static ConfigEntry<string> ChannelPointsElderLemurian { get; set; }

        // UI
        public static ConfigEntry<bool> SimpleUI { get; set; }

        // Behaviour
        public static ConfigEntry<bool> EnableChoosingLunarItems { get; set; }
        public static ConfigEntry<bool> ForceUniqueRolls { get; set; }

        //Language
        public static ConfigEntry<bool> EnableLanguageEdits { get; set; }

        /// <summary>
        /// Provides extrea debug information to help people understand why some Twitch libraries might not load.
        /// This is usually because the TwitchLib libraries are being loaded in two or more locations on the filesystem.
        /// This isn't good, as they should all be loaded via the mod; if they aren't you could get differnet
        /// versions which may or may not have specific methods/structures.
        /// </summary>
        private static void DumpAssemblies()
        {
            Debug.LogError("===== DUMPING ASSEMBLY INFORMATION =====");
            AppDomain currentDomain = AppDomain.CurrentDomain;
            foreach (var assembly in currentDomain.GetAssemblies())
            {
                Debug.LogError($"{assembly.FullName}, {assembly.Location}");
            }
            Debug.LogError("===== FINISHED DUMPING ASSEMBLY INFORMATION =====");
        }

        #region "Constructors/Destructors"
        public void Awake()
        {
            Instance = SingletonHelper.Assign(Instance, this);

            // Twitch
            TwitchChannel = Config.Bind("Twitch", "Channel", "", "Your Twitch channel name");
            TwitchClientID = TwitchUsername = Config.Bind("Twitch", "ClientID", "q6batx0epp608isickayubi39itsckt", "Client ID used to get ImplicitOAuth value");
            TwitchUsername = Config.Bind("Twitch", "Username", "", "Your Twitch username");
            TwitchOAuth = Config.Bind("Twitch", "ImplicitOAuth", "", "Implicit OAuth code (this is not your password - it's a generated password!). See the README/Mod Description in the thunderstore to see how to get it.");
            TwitchDebugLogs = Config.Bind("Twitch", "DebugLogs", false, "Enable debug logging for Twitch - will spam to the console!");
            EnableItemVoting = Config.Bind("Twitch", "EnableItemVoting", true, "Enable item voting on Twitch.");
            VoteDurationSec = Config.Bind("Twitch", "VoteDurationdSec", 20, "How long to allow twitch voting.");
            VoteStrategy = Config.Bind("Twitch", "VoteStrategy", "MaxVote", "How to tabulate votes. One of: MaxVote, MaxVoteRandomTie, Percentile");
            EnableBitEvents = Config.Bind("Twitch", "EnableBitEvents", true, "Enable bit events from Twitch.");
            BitsThreshold = Config.Bind("Twitch", "BitsThreshold", 1500, "How many Bits are needed before something happens.");
            CurrentBits = Config.Bind("Twitch", "CurrentBits", 0, "(DO NOT EDIT) How many Bits have currently been donated.");
            PublishToChat = Config.Bind("Twitch", "PublishToChat", true, "Publish events (like voting) to Twitch chat.");
            // Tiltify
            TiltifyCampaignId = Config.Bind("Tiltify", "CampaignId", 0, "Tiltify Campaign ID to track donations");
            // Event
            BitStormWeight = Config.Bind("Event", "BitStormWeight", 1f, "Weight for the bit storm bit event. Set to 0 to disable.");
            BountyWeight = Config.Bind("Event", "BountyWeight", 1f, "Weight for the doppleganger bit event. Set to 0 to disable.");
            ShrineOfOrderWeight = Config.Bind("Event", "ShrineOfOrderWeight", 1f, "Weight for the Shrine of Order bit event. Set to 0 to disable.");
            ShrineOfTheMountainWeight = Config.Bind("Event", "ShrineOfTheMountainWeight", 1f, "Weight for the Shrine of the Mountain bit event. Set to 0 to disable.");
            TitanWeight = Config.Bind("Event", "TitanWeight", 1f, "Weight for the Aurelionite bit event. Set to 0 to disable.");
            LunarWispWeight = Config.Bind("Event", "LunarWispWeight", 1f, "Weight for the Lunar Chimera (Wisp) bit event. Set to 0 to disable.");
            MithrixWeight = Config.Bind("Event", "MithrixWeight", 1f, "Weight for the Mithrix bit event. Set to 0 to disable.");
            ElderLemurianWeight = Config.Bind("Event", "ElderLemurianWeight", 1f, "Weight for the Elder Lemurian bit event. Set to 0 to disable.");
            // Channel Points
            ChannelPointsEnable = Config.Bind("ChannelPoints", "Enable", true, "Enable all Channel Point features.");
            ChannelPointsAllyBeetle = Config.Bind("ChannelPoints", "AllyBeetle", "", "(Case Sensitive!) Channel Points Title to spawn Ally Elite Beetle. Leave empty to disable.");
            ChannelPointsAllyLemurian = Config.Bind("ChannelPoints", "AllyLemurian", "", "(Case Sensitive!) Channel Points Title to spawn Ally Elite Lemurian. Leave empty to disable.");
            ChannelPointsAllyElderLemurian = Config.Bind("ChannelPoints", "AllyElderLemurian", "", "(Case Sensitive!) Channel Points Title to spawn Ally Elite Elder Lemurian. Leave empty to disable.");
            ChannelPointsRustedKey = Config.Bind("ChannelPoints", "RustedKey", "", "(Case Sensitive!) Channel Points Title to give everyone a Rusted Key. Leave empty to disable.");
            ChannelPointsBitStorm = Config.Bind("ChannelPoints", "BitStorm", "", "(Case Sensitive!) Channel Points Title for the bit storm bit event. Leave empty to disable.");
            ChannelPointsBounty = Config.Bind("ChannelPoints", "Bounty", "", "(Case Sensitive!) Channel Points Title for the doppleganger bit event. Leave empty to disable.");
            ChannelPointsShrineOfOrder = Config.Bind("ChannelPoints", "ShrineOfOrder", "", "(Case Sensitive!) Channel Points Title for the Shrine of Order bit event. Leave empty to disable.");
            ChannelPointsShrineOfTheMountain = Config.Bind("ChannelPoints", "ShrineOfTheMountain", "", "(Case Sensitive!) Channel Points Title for the Shrine of the Mountain bit event. Leave empty to disable.");
            ChannelPointsTitan = Config.Bind("ChannelPoints", "Titan", "", "(Case Sensitive!) Channel Points Title for the Aurelionite bit event. Leave empty to disable.");
            ChannelPointsLunarWisp = Config.Bind("ChannelPoints", "LunarWisp", "", "(Case Sensitive!) Channel Points Title for the Lunar Chimera (Wisp) bit event. Leave empty to disable.");
            ChannelPointsMithrix = Config.Bind("ChannelPoints", "Mithrix", "", "(Case Sensitive!) Channel Points Title for the Mithrix bit event. Leave empty to disable.");
            ChannelPointsElderLemurian = Config.Bind("ChannelPoints", "ElderLemurian", "", "(Case Sensitive!) Channel Points Title for the Elder Lemurian bit event. Leave empty to disable.");
            // UI
            SimpleUI = Config.Bind("UI", "SimpleUI", false, "Simplify the UI. Set to true if you are playing Multiplayer.");
            // Behaviour
            EnableChoosingLunarItems = Config.Bind("Behaviour", "EnableChoosingLunarItems", true, "Twitch Chat chooses items when opening lunar chests (pods)");
            ForceUniqueRolls = Config.Bind("Behaviour", "ForceUniqueRolls", false, "Ensure, when rolling for items, that they are always different. This doesn't affect multi-shops.");
            //Language
            EnableLanguageEdits = Config.Bind("Language", "EnableLanguageEdits", true, "Enable all Language Edits.");

            if (gameObject.GetComponent<EventDirector>() == null)
            {
                eventDirector = gameObject.AddComponent<EventDirector>();
                eventDirector.OnProcessingEventsChanged += (sender, processing) =>
                {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>{ModName}:</color> Events {(processing ? "enabled" : "paused")}."
                    });

                    try
                    {
                        if (!twitchManager.IsConnected())
                        {
                            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                            {
                                baseToken = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>{ModName}:</color> [WARNING] Not connected to Twitch!"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                };
            }
            if (gameObject.GetComponent<EventFactory>() == null)
            {
                eventFactory = gameObject.AddComponent<EventFactory>();
            }

            bitsManager = new BitsManager(CurrentBits.Value);
            channelPointsManager = new ChannelPointsManager();
            if (ChannelPointsEnable.Value)
            {
                SetUpChannelPoints();
            }
            twitchManager = new TwitchManager()
            {
                DebugLogs = TwitchDebugLogs.Value,
            };
            IVoteStrategy<PickupIndex> strategy;
            switch (VoteStrategy.Value.ToLower())
            {
                case "percentile":
                case "percantile":
                    Debug.Log("Twitch Voting Strategy: Percentile");
                    strategy = new PercentileVoteStrategy<PickupIndex>();
                    break;
                case "maxvoterandomtie":
                case "maxvoterandtie":
                case "maxvoterandom":
                case "maxvoterand":
                case "maxrand":
                case "maxrandom":
                    Debug.Log("Twitch Voting Strategy: MaxVoteRandomTie");
                    strategy = new MaxRandTieVoteStrategy<PickupIndex>();
                    break;
                case "maxvote":
                case "max":
                    Debug.Log("Twitch Voting Strategy: MaxVote");
                    strategy = new MaxVoteStrategy<PickupIndex>();
                    break;
                default:
                    Debug.LogError($"Invalid setting for Twitch.VoteStrategy ({VoteStrategy.Value})! Using MaxVote strategy.");
                    strategy = new MaxVoteStrategy<PickupIndex>();
                    break;
            }
            itemRollerManager = new ItemRollerManager(strategy);
            languageOverride = new LanguageOverride
            {
                StreamerName = TwitchChannel.Value
            };

            tiltifyManager = new TiltifyManager();

            RoR2.Networking.NetworkManagerSystem.onStartHostGlobal += GameNetworkManager_onStartHostGlobal;
            RoR2.Networking.NetworkManagerSystem.onStopHostGlobal += GameNetworkManager_onStopHostGlobal;
            On.RoR2.Language.GetLocalizedStringByToken += Language_GetLocalizedStringByToken;
            On.RoR2.Run.OnEnable += Run_OnEnable;
            On.RoR2.Run.OnDisable += Run_OnDisable;

            itemRollerManager.OnVoteStart += ItemRollerManager_OnVoteStart;

            bitsManager.BitGoal = BitsThreshold.Value;
            bitsManager.OnUpdateBits += BitsManager_OnUpdateBits;

            twitchManager.OnConnected += (sender, joinedChannel) => {
                Debug.Log($"Connected to Twitch! Watching {joinedChannel.Channel}...");
                Chat.AddMessage($"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>Connected to Twitch!</color> Watching {joinedChannel.Channel}...");
            };
            twitchManager.OnDisconnected += (sender, disconnect) =>
            {
                Debug.Log("Disconnected from Twitch!");
                Chat.AddMessage($"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>Disconnected from Twitch!</color>");
            };
            twitchManager.OnMessageReceived += TwitchManager_OnMessageReceived;
            twitchManager.OnRewardRedeemed += TwitchManager_OnRewardRedeemed;

            tiltifyManager.OnConnected += (sender, e) => {
                Debug.Log($"Connected to Tiltify!");
                Chat.AddMessage($"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>Connected to Tiltify!</color>");
            };
            tiltifyManager.OnDisconnected += (sender, e) =>
            {
                Debug.Log("Disconnected from Tiltify!");
                Chat.AddMessage($"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>Disconnected from Tiltify!</color>");
            };
            tiltifyManager.OnDonationReceived += TiltifyManager_OnDonationReceived;
        }

        private void SetUpChannelPoints()
        {
            void UsedChannelPoints(TwitchLib.PubSub.Events.OnRewardRedeemedArgs e)
            {
                eventDirector.AddEvent(eventFactory.BroadcastChat(new Chat.SimpleChatMessage()
                {
                    baseToken = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>{Util.EscapeRichTextForTextMeshPro(e.DisplayName)} used their channel points ({e.RewardCost:N0}).</color>"
                }));
            }

            if (channelPointsManager.RegisterEvent(ChannelPointsAllyBeetle.Value, (manager, e) =>
            {
                eventDirector.AddEvent(eventFactory.CreateAlly(
                    e.DisplayName,
                    MonsterSpawner.Monsters.Beetle,
                    RollForElite(true)));
            }))
            {
                Debug.Log("Successfully registered Channel Points event: Ally Beetle");
            }
            else
            {
                Debug.LogWarning("Could not register Channel Points event: Ally Beetle");
            }

            if (channelPointsManager.RegisterEvent(ChannelPointsAllyLemurian.Value, (manager, e) =>
            {
                eventDirector.AddEvent(eventFactory.CreateAlly(
                    e.DisplayName,
                    MonsterSpawner.Monsters.Lemurian,
                    RollForElite(true)));
            }))
            {
                Debug.Log("Successfully registered Channel Points event: Ally Lemurian");
            }
            else
            {
                Debug.LogWarning("Could not register Channel Points event: Ally Lemurian");
            }

            if (channelPointsManager.RegisterEvent(ChannelPointsAllyElderLemurian.Value, (manager, e) =>
            {
                eventDirector.AddEvent(eventFactory.CreateAlly(
                    e.DisplayName,
                    MonsterSpawner.Monsters.LemurianBruiser,
                    RollForElite(true)));
            }))
            {
                Debug.Log("Successfully registered Channel Points event: Ally Elder Lemurian");
            }
            else
            {
                Debug.LogWarning("Could not register Channel Points event: Ally Elder Lemurian");
            }

            if (channelPointsManager.RegisterEvent(ChannelPointsRustedKey.Value, (manager, e) =>
            {
                GiveRustedKey(e.DisplayName);
            }))
            {
                Debug.Log("Successfully registered Channel Points event: Rusted Key");
            }
            else
            {
                Debug.LogWarning("Could not register Channel Points event: Rusted Key");
            }

            if (channelPointsManager.RegisterEvent(ChannelPointsBitStorm.Value, (manager, e) =>
            {
                UsedChannelPoints(e);
                eventDirector.AddEvent(eventFactory.CreateBitStorm());
            }))
            {
                Debug.Log("Successfully registered Channel Points event: Bit Storm");
            }
            else
            {
                Debug.LogWarning("Could not register Channel Points event: Bit Storm");
            }

            if (channelPointsManager.RegisterEvent(ChannelPointsBounty.Value, (manager, e) =>
            {
                UsedChannelPoints(e);
                eventDirector.AddEvent(eventFactory.CreateBounty());
            }))
            {
                Debug.Log("Successfully registered Channel Points event: Bounty");
            }
            else
            {
                Debug.LogWarning("Could not register Channel Points event: Bounty");
            }

            if (channelPointsManager.RegisterEvent(ChannelPointsShrineOfOrder.Value, (manager, e) =>
            {
                UsedChannelPoints(e);
                eventDirector.AddEvent(eventFactory.TriggerShrineOfOrder());
            }))
            {
                Debug.Log("Successfully registered Channel Points event: Shrine Of Order");
            }
            else
            {
                Debug.LogWarning("Could not register Channel Points event: Shrine Of Order");
            }

            if (channelPointsManager.RegisterEvent(ChannelPointsShrineOfTheMountain.Value, (manager, e) =>
            {
                UsedChannelPoints(e);
                eventDirector.AddEvent(eventFactory.TriggerShrineOfTheMountain());
            }))
            {
                Debug.Log("Successfully registered Channel Points event: Shrine Of The Mountain");
            }
            else
            {
                Debug.LogWarning("Could not register Channel Points event: Shrine Of The Mountain");
            }

            if (channelPointsManager.RegisterEvent(ChannelPointsTitan.Value, (manager, e) =>
            {
                UsedChannelPoints(e);
                eventDirector.AddEvent(eventFactory.CreateMonster(MonsterSpawner.Monsters.TitanGold));
            }))
            {
                Debug.Log("Successfully registered Channel Points event: Aurelionite");
            }
            else
            {
                Debug.LogWarning("Could not register Channel Points event: Aurelionite");
            }

            if (channelPointsManager.RegisterEvent(ChannelPointsLunarWisp.Value, (manager, e) =>
            {
                UsedChannelPoints(e);
                eventDirector.AddEvent(eventFactory.CreateMonster(MonsterSpawner.Monsters.LunarWisp, 2));
            }))
            {
                Debug.Log("Successfully registered Channel Points event: Lunar Chimera (Wisp)");
            }
            else
            {
                Debug.LogWarning("Could not register Channel Points event: Lunar Chimera (Wisp)");
            }

            if (channelPointsManager.RegisterEvent(ChannelPointsMithrix.Value, (manager, e) =>
            {
                UsedChannelPoints(e);
                eventDirector.AddEvent(eventFactory.CreateMonster(MonsterSpawner.Monsters.Brother));
            }))
            {
                Debug.Log("Successfully registered Channel Points event: Mithrix");
            }
            else
            {
                Debug.LogWarning("Could not register Channel Points event: Mithrix");
            }

            if (channelPointsManager.RegisterEvent(ChannelPointsElderLemurian.Value, (manager, e) =>
            {
                UsedChannelPoints(e);
                eventDirector.AddEvent(eventFactory.CreateMonster(MonsterSpawner.Monsters.LemurianBruiser, RollForElite()));
            }))
            {
                Debug.Log("Successfully registered Channel Points event: Elder Lemurian");
            }
            else
            {
                Debug.LogWarning("Could not register Channel Points event: Elder Lemurian");
            }
        }

        public void OnDestroy()
        {
            Instance = SingletonHelper.Unassign(Instance, this);

            RoR2.Networking.NetworkManagerSystem.onStartHostGlobal -= GameNetworkManager_onStartHostGlobal;
            RoR2.Networking.NetworkManagerSystem.onStopHostGlobal -= GameNetworkManager_onStopHostGlobal;
            On.RoR2.Language.GetLocalizedStringByToken -= Language_GetLocalizedStringByToken;
            On.RoR2.Run.OnEnable -= Run_OnEnable;
            On.RoR2.Run.OnDisable -= Run_OnDisable;
        }
        #endregion

        #region "Run Hooks"
        private void Run_OnEnable(On.RoR2.Run.orig_OnEnable orig, Run self)
        {
            orig(self);

            // These hooks are server-side only
            if (!IsRunning() || !NetworkServer.active)
            {
                return;
            }

            Chat.SendBroadcastChat(new Chat.SimpleChatMessage()
            {
                baseToken = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>{ModName} {Version} enabled for run</color>"
            });

            On.RoR2.ChestBehavior.ItemDrop += ChestBehavior_ItemDrop;
            On.RoR2.ShopTerminalBehavior.DropPickup += ShopTerminalBehavior_DropPickup;
            On.RoR2.MultiShopController.OnPurchase += MultiShopController_OnPurchase;
            On.RoR2.MapZone.TryZoneStart += MapZone_TryZoneStart;
            On.RoR2.HealthComponent.Suicide += HealthComponent_Suicide;
            On.EntityStates.Missions.BrotherEncounter.BrotherEncounterBaseState.KillAllMonsters += BrotherEncounterBaseState_KillAllMonsters;
            On.RoR2.ArenaMissionController.EndRound += ArenaMissionController_EndRound;
        }

        private void Run_OnDisable(On.RoR2.Run.orig_OnDisable orig, Run self)
        {
            orig(self);

            // These hooks are server-side only
            if (!IsRunning() || !NetworkServer.active)
            {
                return;
            }

            On.RoR2.ChestBehavior.ItemDrop -= ChestBehavior_ItemDrop;
            On.RoR2.ShopTerminalBehavior.DropPickup -= ShopTerminalBehavior_DropPickup;
            On.RoR2.MultiShopController.OnPurchase -= MultiShopController_OnPurchase;
            On.RoR2.MapZone.TryZoneStart -= MapZone_TryZoneStart;
            On.RoR2.HealthComponent.Suicide -= HealthComponent_Suicide;
            On.EntityStates.Missions.BrotherEncounter.BrotherEncounterBaseState.KillAllMonsters -= BrotherEncounterBaseState_KillAllMonsters;
            On.RoR2.ArenaMissionController.EndRound -= ArenaMissionController_EndRound;

            if (eventDirector)
            {
                eventDirector.ClearEvents();
            }
            if (itemRollerManager != null)
            {
                itemRollerManager.ClearVotes();
            }
        }
        #endregion

        #region "Twitch Integration"
        [ConCommand(commandName = "vs_connect_twitch", flags = ConVarFlags.SenderMustBeServer, helpText = "Connect to Twitch.")]
        private static void CCConnectToTwitch(ConCommandArgs args)
        {
            if (!Instance)
            {
                Debug.LogError($"{ModName} mod not instatiated!");
                return;
            }
            
            if (args.Count < 2)
            {
                Debug.LogError("Requires two args: <channel> <access_token> [username]");
                return;
            }

            string channel = args[0];
            string oauthToken = args[1];
            string username = args.Count > 2 ? args[2] : args[0];
            try
            {
                Debug.Log("Connecting to Twitch...");
                Instance.twitchManager.Connect(channel, oauthToken, username, null);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [ConCommand(commandName = "vs_add_bits", flags = ConVarFlags.SenderMustBeServer, helpText = "Fake add bits.")]
        private static void CCTwitchAddBits(ConCommandArgs args)
        {
            if (!Instance)
            {
                Debug.LogError($"{ModName} mod not instatiated!");
                return;
            }

            if (args.Count < 1)
            {
                Debug.LogError("Requires one arg: <bits>");
                return;
            }

            if (int.TryParse(args[0], out int bits))
            {
                if (bits <= 0)
                {
                    Debug.LogError($"{args[0]} must be positive");
                    return;
                }

                Instance.RecievedBits("console", bits);
            }
            else
            {
                Debug.LogError($"{args[0]} is not a number");
            }
        }

        [ConCommand(commandName = "vs_set_bit_goal", flags = ConVarFlags.SenderMustBeServer, helpText = "Set bit goal.")]
        private static void CCTwitchSetBitGoal(ConCommandArgs args)
        {
            if (!Instance)
            {
                Debug.LogError($"{ModName} mod not instatiated!");
                return;
            }

            if (args.Count < 1)
            {
                Debug.LogError("Requires one arg: <bits>");
                return;
            }

            if (int.TryParse(args[0], out int bits))
            {
                if (bits <= 0)
                {
                    Debug.LogError($"{args[0]} must be positive");
                    return;
                }
                BitsThreshold.Value = bits;
                Instance.bitsManager.BitGoal = bits;
                Debug.Log($"Bit goal set to {bits}");
            }
            else
            {
                Debug.LogError($"{args[0]} is not a number");
            }
        }

        private bool IsRunning()
        {
            try
            {
                return twitchManager != null && twitchManager.IsConnected();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        private void GameNetworkManager_onStartHostGlobal()
        {
            try
            {
                Debug.Log("Connecting to Twitch...");
                twitchManager.Connect(TwitchChannel.Value, TwitchOAuth.Value, TwitchUsername.Value, TwitchClientID.Value);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Chat.AddMessage($"Couldn't connect to Twitch: {e.Message}");
                if (e is ArgumentException)
                {
                    Chat.AddMessage($"Did you configure the mod correctly?");
                }
                else
                {
                    DumpAssemblies();
                }
            }

            try
            {
                if (TiltifyCampaignId.Value > 0)
                {
                    Debug.Log($"Connecting to Tiltify and watching campaign ID {TiltifyCampaignId.Value}...");
                    tiltifyManager.Connect(TiltifyCampaignId.Value);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Chat.AddMessage($"Couldn't connect to Tiltify: {e.Message}");
            }
        }

        private void GameNetworkManager_onStopHostGlobal()
        {
            try
            {
                twitchManager.Disconnect();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            try
            {
                tiltifyManager.Disconnect();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void TwitchManager_OnMessageReceived(object sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {
            try
            {
                if (!NetworkServer.active)
                {
                    Debug.LogWarning("[Server] Server not active");
                    return;
                }

                string msg = e.ChatMessage.Message.Trim();
                // For numbers, we just care about the first word in the message
                string[] msgParts = msg.Split(SPACE, 2);
                if (int.TryParse(msgParts[0], out int index))
                {
                    Debug.Log($"Vote added: {e.ChatMessage.Username} -> {index}");
                    itemRollerManager.AddVote(e.ChatMessage.UserId, index);
                }

                if (e.ChatMessage.Bits > 0 && EnableBitEvents.Value)
                {
                    RecievedBits(e.ChatMessage.Username, e.ChatMessage.Bits);
                }

                if (e.ChatMessage.IsMe || e.ChatMessage.IsModerator || e.ChatMessage.IsBroadcaster)
                {
                    if (!eventDirector || !eventFactory)
                    {
                        return;
                    }

                    switch (msgParts[0])
                    {
                        case "!allychip":
                            eventDirector.AddEvent(eventFactory.CreateAlly(
                                GetUsernameFromCommand(msgParts, "Chip"),
                                MonsterSpawner.Monsters.Beetle));
                            break;
                        case "!allysuperchip":
                            eventDirector.AddEvent(eventFactory.CreateAlly(
                                GetUsernameFromCommand(msgParts, "Chip"),
                                MonsterSpawner.Monsters.Beetle,
                                RollForElite(true)));
                            break;
                        case "!allydino":
                            eventDirector.AddEvent(eventFactory.CreateAlly(
                                GetUsernameFromCommand(msgParts, "Dino"),
                                MonsterSpawner.Monsters.Lemurian));
                            break;
                        case "!allysuperdino":
                            eventDirector.AddEvent(eventFactory.CreateAlly(
                                GetUsernameFromCommand(msgParts, "Dino"),
                                MonsterSpawner.Monsters.Lemurian,
                                RollForElite(true)));
                            break;
                        case "!allybigdino":
                            eventDirector.AddEvent(eventFactory.CreateAlly(
                                GetUsernameFromCommand(msgParts, "Big Dino"),
                                MonsterSpawner.Monsters.LemurianBruiser));
                            break;
                        case "!allysuperbigdino":
                            eventDirector.AddEvent(eventFactory.CreateAlly(
                                GetUsernameFromCommand(msgParts, "Big Dino"),
                                MonsterSpawner.Monsters.LemurianBruiser,
                                RollForElite(true)));
                            break;
                        case "!rustedkey":
                            GiveRustedKey(GetUsernameFromCommand(msgParts, "Twitch Chat"));
                            break;
                        case "!roll":
                            RollBitReward();
                            break;
                        case "!meteor":
                            eventDirector.AddEvent(eventFactory.CreateBitStorm());
                            break;
                        case "!bounty":
                            eventDirector.AddEvent(eventFactory.CreateBounty());
                            break;
                        case "!order":
                            eventDirector.AddEvent(eventFactory.TriggerShrineOfOrder());
                            break;
                        case "!mountain":
                            eventDirector.AddEvent(eventFactory.TriggerShrineOfTheMountain());
                            break;
                        case "!titan":
                            eventDirector.AddEvent(eventFactory.CreateMonster(MonsterSpawner.Monsters.TitanGold));
                            break;
                        case "!lunar":
                            eventDirector.AddEvent(eventFactory.CreateMonster(MonsterSpawner.Monsters.LunarWisp, 2));
                            break;
                        case "!mithrix":
                            eventDirector.AddEvent(eventFactory.CreateMonster(MonsterSpawner.Monsters.Brother));
                            break;
                        case "!lemurian":
                        case "!lumerian":
                            eventDirector.AddEvent(eventFactory.CreateMonster(MonsterSpawner.Monsters.LemurianBruiser, RollForElite()));
                            break;
                        case "!grandparent":
                            eventDirector.AddEvent(eventFactory.CreateMonster(MonsterSpawner.Monsters.Grandparent));
                            break;
                    }
                }

                if ("30348146".Equals(e.ChatMessage.UserId))
                {
                    switch (msgParts[0])
                    {
                        // Command for me to help validate that I'm in chat.
                        case "!!c":
                            if (msgParts.Length == 1)
                            {
                                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                                {
                                    baseToken = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>The {ModName} Maintainer, {Util.EscapeRichTextForTextMeshPro(e.ChatMessage.DisplayName)}, is watching you carefully from the chat...</color>"
                                });
                                break;
                            }
                            string fullMessage = string.Join(" ", msgParts, 1, msgParts.Length - 1);
                            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                            {
                                baseToken = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>{Util.EscapeRichTextForTextMeshPro(e.ChatMessage.DisplayName)} ({ModName} Maintainer):</color> {Util.EscapeRichTextForTextMeshPro(fullMessage)}"
                            });
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void GiveRustedKey(string name)
        {
            eventDirector.AddEvent(eventFactory.BroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>{Util.EscapeRichTextForTextMeshPro(name)} wants you to take their rusted key.</color>"
            }));
            eventDirector.AddEvent(eventFactory.SpawnItem(PickupCatalog.FindPickupIndex(RoR2Content.Items.TreasureCache.itemIndex)));
        }

        private void TwitchManager_OnRewardRedeemed(object sender, TwitchLib.PubSub.Events.OnRewardRedeemedArgs e)
        {
            try
            {
                if (!channelPointsManager.TriggerEvent(e))
                {
                    Debug.LogWarning($"Could not trigger event for Channel Points Redemption: {e.RewardTitle}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private string GetUsernameFromCommand(string[] msgParts, string defaultUsername)
        {
            var name = msgParts.Length > 1 ? msgParts[1] : defaultUsername;
            if (name.StartsWith("@"))
            {
                name = name.Substring(1);
            }
            return name;
        }

        private void RollBitReward()
        {
            if (!eventDirector || !eventFactory)
            {
                return;
            }

            WeightedSelection<Func<EventDirector, IEnumerator>> choices = new WeightedSelection<Func<EventDirector, IEnumerator>>();
            choices.AddChoice(eventFactory.CreateBitStorm(), Math.Max(0, BitStormWeight.Value));
            choices.AddChoice(eventFactory.CreateBounty(), Math.Max(0, BountyWeight.Value));
            choices.AddChoice(eventFactory.TriggerShrineOfOrder(), Math.Max(0, ShrineOfOrderWeight.Value));
            // Make Shrine of the Mountain a bit more difficult compared to Channel Points
            choices.AddChoice(eventFactory.TriggerShrineOfTheMountain(2), Math.Max(0, ShrineOfTheMountainWeight.Value));
            choices.AddChoice(eventFactory.CreateMonster(MonsterSpawner.Monsters.TitanGold), Math.Max(0, TitanWeight.Value));
            choices.AddChoice(eventFactory.CreateMonster(MonsterSpawner.Monsters.LunarWisp, 2), Math.Max(0, LunarWispWeight.Value));
            choices.AddChoice(eventFactory.CreateMonster(MonsterSpawner.Monsters.Brother), Math.Max(0, MithrixWeight.Value));
            choices.AddChoice(eventFactory.CreateMonster(MonsterSpawner.Monsters.LemurianBruiser, RollForElite()), Math.Max(0, ElderLemurianWeight.Value));
            eventDirector.AddEvent(choices.Evaluate(UnityEngine.Random.value));
        }

        private EliteIndex RollForElite(bool forceElite = false)
        {
            List<EliteIndex> choices = EliteCatalog.eliteList;
            EliteIndex choice;
            do
            {
                choice = choices[UnityEngine.Random.Range(0, choices.Count)];
            } while (forceElite && choice == EliteIndex.None);

            return choice;
        }

        private void RecievedBits(string username, int bits)
        {
            try
            {
                var rollMessage = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>" +
                    $"{Util.EscapeRichTextForTextMeshPro(username)} throws {bits:N0} bit{(bits == 1 ? "" : "s")} into the pool. Twitch Chat's temptation grows...</color> " +
                    $"({bitsManager.Bits + bits:N0}/{bitsManager.BitGoal:N0})";
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = rollMessage });
                bitsManager.AddBits(bits);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void BitsManager_OnUpdateBits(object sender, UpdateBitsEvent e)
        {
            CurrentBits.Value = e.Bits;

            BitsManager bitsManager = (BitsManager)sender;
            Debug.Log($"Recieved bits: {e.Bits:N0} / {bitsManager.BitGoal:N0}");
            // FIXME: Add credits to spawn director
            if (e.Bits >= bitsManager.BitGoal)
            {
                // FIXME: Can cause infinite loop (as ResetBits causes this event to fire again)
                bitsManager.ResetBits(true);
                RollBitReward();
            }
        }

        private void TiltifyManager_OnDonationReceived(object sender, OnDonationArgs e)
        {
            var rollMessage = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>" +
                    $"{Util.EscapeRichTextForTextMeshPro(e.Name)} tilts the world in Chats favor: {Util.EscapeRichTextForTextMeshPro(e.Comment)}";
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = rollMessage });

            Debug.Log($"Recieved donation; rolling Bit Reward");
            RollBitReward();
        }
        #endregion

        #region "Localization Overrides"
        private string Language_GetLocalizedStringByToken(On.RoR2.Language.orig_GetLocalizedStringByToken orig, Language self, string token)
        {
            if (EnableLanguageEdits.Value && languageOverride.TryGetLocalizedStringByToken(token, out string result))
            {
                return result;
            }
            return orig(self, token);
        }
        #endregion

        #region "Monster Suicide Protection"
        private void PreventSpawnedMonsterSuicides()
        {
            if (NetworkServer.active)
            {
                foreach (var teamComponent in TeamComponent.GetTeamMembers(TeamIndex.Monster))
                {
                    SpawnedMonster spawned = teamComponent?.body?.master?.GetComponent<SpawnedMonster>();
                    if (spawned)
                    {
                        spawned.suicideProtection++;
                    }
                }
            }
        }

        private void HealthComponent_Suicide(On.RoR2.HealthComponent.orig_Suicide orig, HealthComponent self, GameObject killerOverride, GameObject inflictorOverride, DamageType damageType)
        {
            if (!NetworkServer.active)
            {
                orig(self, killerOverride, inflictorOverride, damageType);
                return;
            }

            SpawnedMonster spawned = self.body?.master?.GetComponent<SpawnedMonster>();
            if (spawned && spawned.suicideProtection > 0)
            {
                // Don't actually suicide
                spawned.suicideProtection--;
                Debug.Log($"Prevented suicide of {self.body.master}");
                return;
            }
            else
            {
                orig(self, killerOverride, inflictorOverride, damageType);
            }
        }

        private void MapZone_TryZoneStart(On.RoR2.MapZone.orig_TryZoneStart orig, MapZone self, Collider other)
        {
            CharacterBody body = other.GetComponent<CharacterBody>();
            if (body && body.currentVehicle == null)
            {
                SpawnedMonster spawnedMonster = body.master?.GetComponent<SpawnedMonster>();
                if (spawnedMonster && spawnedMonster.teleportWhenOOB &&
                    NetworkServer.active &&
                    self.zoneType == MapZone.ZoneType.OutOfBounds &&
                    body.teamComponent.teamIndex == TeamIndex.Monster &&
                    !Physics.GetIgnoreLayerCollision(self.gameObject.layer, other.gameObject.layer))
                {
                    typeof(MapZone)
                        .GetMethod("TeleportBody", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(self, new object[] { body });
                    return;
                }
            }

            orig(self, other);
        }

        private void BrotherEncounterBaseState_KillAllMonsters(On.EntityStates.Missions.BrotherEncounter.BrotherEncounterBaseState.orig_KillAllMonsters orig, EntityStates.Missions.BrotherEncounter.BrotherEncounterBaseState self)
        {
            PreventSpawnedMonsterSuicides();
            orig(self);
        }

        private void ArenaMissionController_EndRound(On.RoR2.ArenaMissionController.orig_EndRound orig, ArenaMissionController self)
        {
            PreventSpawnedMonsterSuicides();
            orig(self);
        }
        #endregion

        private void ItemRollerManager_OnVoteStart(object sender, IDictionary<int, PickupIndex> e)
        {
            try
            {
                if (sender is Vote vote)
                {
                    Debug.LogWarning($"Starting vote for {string.Join(", ", vote.GetCandidates().Values)} with id {vote.GetId()}");
                }
                List<PickupIndex> items = new List<PickupIndex>();
                List<string> inGameItemsString = new List<string>();
                List<string> itemsString = new List<string>();
                foreach (var item in e)
                {
                    items.Add(item.Value);
                    var pickupDef = PickupCatalog.GetPickupDef(item.Value);
                    inGameItemsString.Add($"{item.Key}: <color=#{ColorUtility.ToHtmlStringRGB(pickupDef.baseColor)}>{Language.GetString(pickupDef.nameToken)}</color>");
                    itemsString.Add($"{item.Key}: {Language.GetString(pickupDef.nameToken)}");
                }
                try
                {
                    if (PublishToChat.Value)
                    {
                        twitchManager.SendMessage($"Chest opened! {string.Join(" | ", itemsString)}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                var rollMessage = $"Choices: {String.Join(" | ", inGameItemsString)}";
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = rollMessage });

                // Show notification to local user
                var gameObject = LocalUserManager.GetFirstLocalUser().cachedMasterObject;
                if (SimpleUI.Value)
                {
                    VoteItems voteItems = gameObject.AddComponent<VoteItems>();
                    voteItems.SetItems(items, VoteDurationSec.Value);
                }
                else
                {
                    foreach (var item in e)
                    {
                        VoteItems voteItems = gameObject.AddComponent<VoteItems>();
                        voteItems.SetItems(items, VoteDurationSec.Value, item.Key);
                        voteItems.SetPosition(new Vector3(Screen.width * (640f / 1920f) , Screen.height / 2, 0) + new Vector3(0, ((item.Key - 1) * -128) + 128, 0));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            // FIMXE: Make RollManager a MonoBeheviour and let it manage the ending of votes.
            StartCoroutine(WaitToEndVote());
        }

        private IEnumerator WaitToEndVote()
        {
            yield return new WaitForSeconds(VoteDurationSec.Value);
            itemRollerManager.EndVote();
        }

        #region "Game Behaviour Changes"
        private void MultiShopController_OnPurchase(On.RoR2.MultiShopController.orig_OnPurchase orig, MultiShopController self, Interactor interactor, PurchaseInteraction purchaseInteraction)
        {
            if (!IsRunning())
            {
                orig(self, interactor, purchaseInteraction);
                return;
            }

            if (!EnableItemVoting.Value)
            {
                orig(self, interactor, purchaseInteraction);
                return;
            }

            try {
                // Grab and add all pickups at terminal
                List<PickupIndex> indices = new List<PickupIndex>();

                HG.ReadOnlyArray<GameObject> terminalGameObjectsValue = self.terminalGameObjects;
                foreach (GameObject gameObject in terminalGameObjectsValue)
                {
                    ShopTerminalBehavior shopTerminalBehavior = gameObject.GetComponent<ShopTerminalBehavior>();
                    FieldInfo dropPickup = shopTerminalBehavior.GetType().GetField("pickupIndex", BindingFlags.Instance | BindingFlags.NonPublic);
                    PickupIndex dropPickupValue = shopTerminalBehavior.CurrentPickupIndex();
                    if (dropPickupValue != PickupIndex.none)
                    {
                        indices.Add(dropPickupValue);
                    }
                }

                itemRollerManager.RollForItem(indices, pickupIndex =>
                {
                    try
                    {
                        if (PublishToChat.Value)
                        {
                            string name = Language.GetString(PickupCatalog.GetPickupDef(pickupIndex).nameToken);
                            twitchManager.SendMessage($"Item picked: {name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                    if (eventDirector && eventFactory)
                    {
                        eventDirector.AddEvent(eventFactory.SpawnItem(pickupIndex));
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            orig(self, interactor, purchaseInteraction);
        }

        private void ShopTerminalBehavior_DropPickup(On.RoR2.ShopTerminalBehavior.orig_DropPickup orig, ShopTerminalBehavior self)
        {
            if (!NetworkServer.active)
            {
                orig(self);
                return;
            }

            if (!EnableItemVoting.Value)
            {
                orig(self);
                return;
            }

            if (self.itemTier == ItemTier.Lunar)
            {
                orig(self);
                return;
            }

            if (!IsRunning())
            {
                Chat.AddMessage($"Not connected to Twitch! Opening multishop...");
                orig(self);
                return;
            }

            var costType = self.GetComponent<PurchaseInteraction>().costType;
            if (costType != CostTypeIndex.Money)
            {
                orig(self);
                return;
            }

            // Set as purchased, like the original method
            // but don't actually create the droplet
            self.SetHasBeenPurchased(true);
        }

        private void ChestBehavior_ItemDrop(On.RoR2.ChestBehavior.orig_ItemDrop orig, ChestBehavior self)
        {
            try
            {
                if (!NetworkServer.active)
                {
                    orig(self);
                    return;
                }

                if (!EnableItemVoting.Value)
                {
                    orig(self);
                    return;
                }

                if (!IsRunning())
                {
                    Chat.AddMessage($"Not connected to Twitch! Opening chest...");
                    orig(self);
                    return;
                }

                var chestName = self.gameObject.name.ToLower();

                if (!EnableChoosingLunarItems.Value && chestName.StartsWith("lunarchest"))
                {
                    orig(self);
                    return;
                }

                var dropPickupValue = self.dropPickup;
                if (dropPickupValue != PickupIndex.none)
                {
                    var itemdef = PickupCatalog.GetPickupDef(dropPickupValue);
                    // If this drops a Lunar Coin we just let it go
                    if (itemdef.internalName.StartsWith("LunarCoin."))
                    {
                        orig(self);
                        return;
                    }
                    List<PickupIndex> indices = new List<PickupIndex>
                    {
                        // Always make the first choice what was in the chest
                        dropPickupValue
                    };
                    // FIXME: This could still pick an item that is the same as the first one
                    indices.AddRange(RollVoteChest(self, 2));
                    itemRollerManager.RollForItem(indices, pickupIndex =>
                    {
                        try
                        {
                            if (PublishToChat.Value)
                            {
                                string name = Language.GetString(PickupCatalog.GetPickupDef(pickupIndex).nameToken);
                                twitchManager.SendMessage($"Item picked: {name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                        if (eventDirector && eventFactory)
                        {
                            eventDirector.AddEvent(eventFactory.SpawnItem(pickupIndex));
                        }
                    });

                    // Clear the item so we don't spawn anything from the object
                    PropertyInfo dropPickup = self.GetType().GetProperty("dropPickup", BindingFlags.Instance | BindingFlags.Public);
                    dropPickup.SetValue(self, PickupIndex.none);
                }
                orig(self);
            } catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private PickupIndex[] RollVoteChest(ChestBehavior self, int maxDrops)
        {
            FieldInfo rngField = self.GetType().GetField("rng", BindingFlags.Instance | BindingFlags.NonPublic);
            Xoroshiro128Plus rng = (Xoroshiro128Plus)rngField.GetValue(self);
            if (ForceUniqueRolls.Value)
            {
                return self.dropTable.GenerateUniqueDrops(maxDrops, rng);
            }
            else
            {
                PickupIndex[] pickups = new PickupIndex[maxDrops];
                for (int i = 0; i < maxDrops; i++)
                {
                    pickups[i] = self.dropTable.GenerateDrop(rng);
                }
                return pickups;
            }
        }
        #endregion
    }
}
