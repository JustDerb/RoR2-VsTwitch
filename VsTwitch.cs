using BepInEx;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VsTwitch.Twitch;
using VsTwitch.Twitch.WebSocket.Models.Notifications;

// Allow scanning for ConCommand, and other stuff for Risk of Rain 2
[assembly: HG.Reflection.SearchableAttribute.OptIn]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace VsTwitch
{
    [BepInPlugin(GUID, ModName, Version)]
    [BepInDependency(ModCompatibility.RiskOfOptions.ModGUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class VsTwitch : BaseUnityPlugin
    {
        private static readonly char[] SPACE = new char[] { ' ' };
        public const string GUID = "com.justinderby.vstwitch";
        public const string ModName = "VsTwitch";
        public const string Version = "1.1.2";

        // This is only used for ConCommands, since they need to be static...
        public static VsTwitch Instance;

        private TwitchManager twitchManager;
        private BitsManager bitsManager;
        private ChannelPointsManager channelPointsManager;
        private ItemRollerManager itemRollerManager;
        private EventDirector eventDirector;
        private EventFactory eventFactory;
        private TiltifyManager tiltifyManager;
        private Configuration configuration;

        /// <summary>
        /// Provides extra debug information to help people understand why some Twitch libraries might not load.
        /// This is usually because the TwitchLib libraries are being loaded in two or more locations on the filesystem.
        /// This isn't good, as they should all be loaded via the mod; if they aren't you could get differnet
        /// versions which may or may not have specific methods/structures.
        /// </summary>
        private static void DumpAssemblies()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("===== DUMPING ASSEMBLY INFORMATION =====");
            AppDomain currentDomain = AppDomain.CurrentDomain;
            foreach (var assembly in currentDomain.GetAssemblies())
            {
                sb.AppendLine($"{assembly.FullName}, {assembly.Location}");
            }
            sb.AppendLine("===== FINISHED DUMPING ASSEMBLY INFORMATION =====");
            Log.Error(sb.ToString());
        }

        #region "Constructors/Destructors"
        public void Awake()
        {
            Log.Init(Logger);
            Instance = SingletonHelper.Assign(Instance, this);

            configuration = new Configuration(this, () =>
            {
                SetUpChannelPoints();
            });

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
                        Log.Error(ex);
                    }
                };
            }
            if (gameObject.GetComponent<EventFactory>() == null)
            {
                eventFactory = gameObject.AddComponent<EventFactory>();
            }

            bitsManager = new BitsManager(configuration.CurrentBits.Value);
            
            SetUpChannelPoints();

            SetupHelper setupHelper = gameObject.GetComponent<SetupHelper>() ?? gameObject.AddComponent<SetupHelper>();
            twitchManager = new TwitchManager(setupHelper)
            {
                DebugLogs = configuration.TwitchDebugLogs.Value,
            };
            configuration.TwitchDebugLogs.SettingChanged += (sender, e) => twitchManager.DebugLogs = configuration.TwitchDebugLogs.Value;

            IVoteStrategy<PickupIndex> strategy;
            switch (configuration.VoteStrategy.Value)
            {
                case Configuration.VoteStrategies.Percentile:
                    Log.Info("Twitch Voting Strategy: Percentile");
                    strategy = new PercentileVoteStrategy<PickupIndex>();
                    break;
                case Configuration.VoteStrategies.MaxVoteRandomTie:
                    Log.Info("Twitch Voting Strategy: MaxVoteRandomTie");
                    strategy = new MaxRandTieVoteStrategy<PickupIndex>();
                    break;
                case Configuration.VoteStrategies.MaxVote:
                    Log.Info("Twitch Voting Strategy: MaxVote");
                    strategy = new MaxVoteStrategy<PickupIndex>();
                    break;
                default:
                    Log.Error($"Invalid setting for Twitch.VoteStrategy ({configuration.VoteStrategy.Value})! Using MaxVote strategy.");
                    strategy = new MaxVoteStrategy<PickupIndex>();
                    break;
            }
            itemRollerManager = new ItemRollerManager(strategy);

            tiltifyManager = new TiltifyManager();

            RoR2.Networking.NetworkManagerSystem.onStartHostGlobal += GameNetworkManager_onStartHostGlobal;
            RoR2.Networking.NetworkManagerSystem.onStopHostGlobal += GameNetworkManager_onStopHostGlobal;
            On.RoR2.Run.OnEnable += Run_OnEnable;
            On.RoR2.Run.OnDisable += Run_OnDisable;

            itemRollerManager.OnVoteStart += ItemRollerManager_OnVoteStart;

            bitsManager.BitGoal = configuration.BitsThreshold.Value;
            bitsManager.OnUpdateBits += BitsManager_OnUpdateBits;

            twitchManager.OnConnected += (sender, joinedChannel) =>
            {
                Log.Info($"Connected to Twitch! Watching {joinedChannel.Channel}...");
                Chat.AddMessage($"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>Connected to Twitch!</color> Watching {joinedChannel.Channel}...");
                return Task.CompletedTask;
            };
            twitchManager.OnDisconnected += (sender, disconnect) =>
            {
                Log.Info("Disconnected from Twitch!");
                Chat.AddMessage($"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>Disconnected from Twitch!</color>");
                return Task.CompletedTask;
            };
            twitchManager.OnMessageReceived += TwitchManager_OnMessageReceived;
            twitchManager.OnRewardRedeemed += TwitchManager_OnRewardRedeemed;

            tiltifyManager.OnConnected += (sender, e) => 
            {
                Log.Info($"Connected to Tiltify!");
                Chat.AddMessage($"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>Connected to Tiltify!</color>");
            };
            tiltifyManager.OnDisconnected += (sender, e) =>
            {
                Log.Info("Disconnected from Tiltify!");
                Chat.AddMessage($"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>Disconnected from Tiltify!</color>");
            };
            tiltifyManager.OnDonationReceived += TiltifyManager_OnDonationReceived;
        }

        private void SetUpChannelPoints()
        {
            void UsedChannelPoints(ChannelPointsCustomRewardRedemptionAddMessage e)
            {
                eventDirector.AddEvent(eventFactory.BroadcastChat(new Chat.SimpleChatMessage()
                {
                    baseToken = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>{Util.EscapeRichTextForTextMeshPro(e.UserName)} used their channel points ({e.Reward.Cost:N0}).</color>"
                }));
            }

            channelPointsManager = new ChannelPointsManager();

            if (channelPointsManager.RegisterEvent(configuration.ChannelPointsAllyBeetle.Value, (manager, e) =>
            {
                eventDirector.AddEvent(eventFactory.CreateAlly(
                    e.UserName,
                    MonsterSpawner.Monsters.Beetle,
                    RollForElite(true)));
            }))
            {
                Log.Info($"Successfully registered Channel Points event: Ally Beetle ({configuration.ChannelPointsAllyBeetle.Value})");
            }
            else
            {
                Log.Warning("Could not register Channel Points event: Ally Beetle");
            }

            if (channelPointsManager.RegisterEvent(configuration.ChannelPointsAllyLemurian.Value, (manager, e) =>
            {
                eventDirector.AddEvent(eventFactory.CreateAlly(
                    e.UserName,
                    MonsterSpawner.Monsters.Lemurian,
                    RollForElite(true)));
            }))
            {
                Log.Info($"Successfully registered Channel Points event: Ally Lemurian ({configuration.ChannelPointsAllyLemurian.Value})");
            }
            else
            {
                Log.Warning("Could not register Channel Points event: Ally Lemurian");
            }

            if (channelPointsManager.RegisterEvent(configuration.ChannelPointsAllyElderLemurian.Value, (manager, e) =>
            {
                eventDirector.AddEvent(eventFactory.CreateAlly(
                    e.UserName,
                    MonsterSpawner.Monsters.LemurianBruiser,
                    RollForElite(true)));
            }))
            {
                Log.Info($"Successfully registered Channel Points event: Ally Elder Lemurian ({configuration.ChannelPointsAllyElderLemurian.Value})");
            }
            else
            {
                Log.Warning("Could not register Channel Points event: Ally Elder Lemurian");
            }

            if (channelPointsManager.RegisterEvent(configuration.ChannelPointsRustedKey.Value, (manager, e) =>
            {
                GiveRustedKey(e.UserName);
            }))
            {
                Log.Info($"Successfully registered Channel Points event: Rusted Key ({configuration.ChannelPointsRustedKey.Value})");
            }
            else
            {
                Log.Warning("Could not register Channel Points event: Rusted Key");
            }

            if (channelPointsManager.RegisterEvent(configuration.ChannelPointsBitStorm.Value, (manager, e) =>
            {
                UsedChannelPoints(e);
                eventDirector.AddEvent(eventFactory.CreateBitStorm());
            }))
            {
                Log.Info($"Successfully registered Channel Points event: Bit Storm ({configuration.ChannelPointsBitStorm.Value})");
            }
            else
            {
                Log.Warning("Could not register Channel Points event: Bit Storm");
            }

            if (channelPointsManager.RegisterEvent(configuration.ChannelPointsBounty.Value, (manager, e) =>
            {
                UsedChannelPoints(e);
                eventDirector.AddEvent(eventFactory.CreateBounty());
            }))
            {
                Log.Info($"Successfully registered Channel Points event: Bounty ({configuration.ChannelPointsBounty.Value})");
            }
            else
            {
                Log.Warning("Could not register Channel Points event: Bounty");
            }

            if (channelPointsManager.RegisterEvent(configuration.ChannelPointsShrineOfOrder.Value, (manager, e) =>
            {
                UsedChannelPoints(e);
                eventDirector.AddEvent(eventFactory.TriggerShrineOfOrder());
            }))
            {
                Log.Info($"Successfully registered Channel Points event: Shrine Of Order ({configuration.ChannelPointsShrineOfOrder.Value})");
            }
            else
            {
                Log.Warning("Could not register Channel Points event: Shrine Of Order");
            }

            if (channelPointsManager.RegisterEvent(configuration.ChannelPointsShrineOfTheMountain.Value, (manager, e) =>
            {
                UsedChannelPoints(e);
                eventDirector.AddEvent(eventFactory.TriggerShrineOfTheMountain());
            }))
            {
                Log.Info($"Successfully registered Channel Points event: Shrine Of The Mountain ({configuration.ChannelPointsShrineOfTheMountain.Value})");
            }
            else
            {
                Log.Warning("Could not register Channel Points event: Shrine Of The Mountain");
            }

            if (channelPointsManager.RegisterEvent(configuration.ChannelPointsTitan.Value, (manager, e) =>
            {
                UsedChannelPoints(e);
                eventDirector.AddEvent(eventFactory.CreateMonster(MonsterSpawner.Monsters.TitanGold));
            }))
            {
                Log.Info($"Successfully registered Channel Points event: Aurelionite ({configuration.ChannelPointsTitan.Value})");
            }
            else
            {
                Log.Warning("Could not register Channel Points event: Aurelionite");
            }

            if (channelPointsManager.RegisterEvent(configuration.ChannelPointsLunarWisp.Value, (manager, e) =>
            {
                UsedChannelPoints(e);
                eventDirector.AddEvent(eventFactory.CreateMonster(MonsterSpawner.Monsters.LunarWisp, 2));
            }))
            {
                Log.Info($"Successfully registered Channel Points event: Lunar Chimera (Wisp) ({configuration.ChannelPointsLunarWisp.Value})");
            }
            else
            {
                Log.Warning("Could not register Channel Points event: Lunar Chimera (Wisp)");
            }

            if (channelPointsManager.RegisterEvent(configuration.ChannelPointsMithrix.Value, (manager, e) =>
            {
                UsedChannelPoints(e);
                eventDirector.AddEvent(eventFactory.CreateMonster(MonsterSpawner.Monsters.Brother));
            }))
            {
                Log.Info($"Successfully registered Channel Points event: Mithrix ({configuration.ChannelPointsMithrix.Value})");
            }
            else
            {
                Log.Warning("Could not register Channel Points event: Mithrix");
            }

            if (channelPointsManager.RegisterEvent(configuration.ChannelPointsElderLemurian.Value, (manager, e) =>
            {
                UsedChannelPoints(e);
                eventDirector.AddEvent(eventFactory.CreateMonster(MonsterSpawner.Monsters.LemurianBruiser, RollForElite()));
            }))
            {
                Log.Info($"Successfully registered Channel Points event: Elder Lemurian ({configuration.ChannelPointsElderLemurian.Value})");
            }
            else
            {
                Log.Warning("Could not register Channel Points event: Elder Lemurian");
            }
        }

        public void OnDestroy()
        {
            Instance = SingletonHelper.Unassign(Instance, this);

            RoR2.Networking.NetworkManagerSystem.onStartHostGlobal -= GameNetworkManager_onStartHostGlobal;
            RoR2.Networking.NetworkManagerSystem.onStopHostGlobal -= GameNetworkManager_onStopHostGlobal;
            //On.RoR2.Language.GetLocalizedStringByToken -= Language_GetLocalizedStringByToken;
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
#if DEBUG
                baseToken = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>{ModName} {Version} (DEBUG) enabled for run</color>"
#else
                baseToken = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>{ModName} {Version} enabled for run</color>"
#endif
            });

            On.RoR2.ChestBehavior.ItemDrop += ChestBehavior_ItemDrop;
            On.RoR2.ShopTerminalBehavior.DropPickup += ShopTerminalBehavior_DropPickup;
            On.RoR2.MultiShopController.OnPurchase += MultiShopController_OnPurchase;
            On.RoR2.MapZone.TryZoneStart += MapZone_TryZoneStart;
            On.RoR2.HealthComponent.Suicide += HealthComponent_Suicide;
            On.EntityStates.Missions.BrotherEncounter.BrotherEncounterBaseState.KillAllMonsters += BrotherEncounterBaseState_KillAllMonsters;
            On.RoR2.ArenaMissionController.EndRound += ArenaMissionController_EndRound;

            //On.RoR2.PurchaseInteraction.OnInteractionBegin += PurchaseInteraction_OnInteractionBegin;
            //On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 += PickupDropletController_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3;
        }

        private void PickupDropletController_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3(On.RoR2.PickupDropletController.orig_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 orig, GenericPickupController.CreatePickupInfo pickupInfo, Vector3 position, Vector3 velocity)
        {
            Log.Error("PickupDropletController_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3: START");
            orig(pickupInfo, position, velocity);
            Log.Error("PickupDropletController_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3: END");
        }

        private void PurchaseInteraction_OnInteractionBegin(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            Log.Error("PurchaseInteraction_OnInteractionBegin: START");
            if (!IsRunning())
            {
                orig(self, activator);
                return;
            }

            if (!configuration.EnableItemVoting.Value)
            {
                orig(self, activator);
                return;
            }

            if ((bool)self.GetComponent<ChestBehavior>())
            {
                var chest = self.GetComponent<ChestBehavior>();
                var dropPickupValue = chest.currentPickup.pickupIndex;
                if (dropPickupValue != PickupIndex.none)
                {
                    var itemdef = PickupCatalog.GetPickupDef(dropPickupValue);
                    Log.Error($"PurchaseInteraction_OnInteractionBegin: ChestBehavior has {itemdef.internalName}");
                }
            }
            else if ((bool)self.GetComponent<MultiShopController>())
            {
                var chest = self.GetComponent<MultiShopController>();
                HG.ReadOnlyArray<GameObject> terminalGameObjectsValue = chest.terminalGameObjects;
                foreach (GameObject gameObject in terminalGameObjectsValue)
                {
                    ShopTerminalBehavior shopTerminalBehavior = gameObject.GetComponent<ShopTerminalBehavior>();
                    FieldInfo dropPickup = shopTerminalBehavior.GetType().GetField("pickupIndex", BindingFlags.Instance | BindingFlags.NonPublic);
                    PickupIndex dropPickupValue = shopTerminalBehavior.CurrentPickup().pickupIndex;
                    if (dropPickupValue != PickupIndex.none)
                    {
                        var itemdef = PickupCatalog.GetPickupDef(dropPickupValue);
                        Log.Error($"PurchaseInteraction_OnInteractionBegin: MultiShopController has {itemdef.internalName}");
                    }
                }
            }
            else if ((bool)self.GetComponent<RouletteChestController>())
            {
                var chest = self.GetComponent<RouletteChestController>();
                var dropPickupValue = chest.pickupDisplay.GetPickupIndex();
                if (dropPickupValue != PickupIndex.none)
                {
                    var itemdef = PickupCatalog.GetPickupDef(dropPickupValue);
                    Log.Error($"PurchaseInteraction_OnInteractionBegin: RouletteChestController has {itemdef.internalName}");
                }
            }

            orig(self, activator);
            Log.Error("PurchaseInteraction_OnInteractionBegin: END");
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

            //On.RoR2.PurchaseInteraction.OnInteractionBegin -= PurchaseInteraction_OnInteractionBegin;
            //On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 -= PickupDropletController_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3;

            eventFactory?.Reset();
            eventDirector?.ClearEvents();
            itemRollerManager?.ClearVotes();
        }
#endregion

        #region "Twitch Integration"
        [ConCommand(commandName = "vs_add_bits", flags = ConVarFlags.SenderMustBeServer, helpText = "Fake add bits.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "ConCommand")]
        private static void CCTwitchAddBits(ConCommandArgs args)
        {
            if (!Instance)
            {
                Debug.LogError($"{ModName} mod not instatiated!");
                return;
            }

            if (args.Count < 1)
            {
                Log.Error("Requires one arg: <bits>");
                return;
            }

            if (int.TryParse(args[0], out int bits))
            {
                if (bits <= 0)
                {
                    Log.Error($"{args[0]} must be positive");
                    return;
                }

                Instance.RecievedBits("console", bits);
            }
            else
            {
                Log.Error($"{args[0]} is not a number");
            }
        }

        [ConCommand(commandName = "vs_set_bit_goal", flags = ConVarFlags.SenderMustBeServer, helpText = "Set bit goal.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "ConCommand")]
        private static void CCTwitchSetBitGoal(ConCommandArgs args)
        {
            if (!Instance)
            {
                Debug.LogError($"{ModName} mod not instatiated!");
                return;
            }

            if (args.Count < 1)
            {
                Log.Error("Requires one arg: <bits>");
                return;
            }

            if (int.TryParse(args[0], out int bits))
            {
                if (bits <= 0)
                {
                    Log.Error($"{args[0]} must be positive");
                    return;
                }
                Instance.configuration.BitsThreshold.Value = bits;
                Instance.bitsManager.BitGoal = bits;
                Log.Info($"Bit goal set to {bits}");
            }
            else
            {
                Log.Error($"{args[0]} is not a number");
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
                Log.Error(ex);
                return false;
            }
        }

        private void GameNetworkManager_onStartHostGlobal()
        {
            Log.Info("Connecting to Twitch...");
            StartCoroutine(ConnectToTwitch());
            
            try
            {
                if (!string.IsNullOrWhiteSpace(configuration.TiltifyCampaignId.Value))
                {
                    Log.Info($"Connecting to Tiltify and watching campaign ID {configuration.TiltifyCampaignId.Value}...");
                    tiltifyManager.Connect(configuration.TiltifyCampaignId.Value);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                Chat.AddMessage($"Couldn't connect to Tiltify: {e.Message}");
            }
        }

        private IEnumerator ConnectToTwitch()
        {
            // We need to wait until the scene finished since the setup could load a dialog box (and we shouldn't load assets during a scene transition)
            yield return new WaitUntil(() => NetworkManager.s_LoadingSceneAsync == null);
            
            yield return twitchManager.MaybeSetup(configuration)
                .ContinueWith(t => {
                    if (t.Exception != null)
                    {
                        return Task.FromException(t.Exception);
                    }
                    return twitchManager.Connect();
                })
                .Unwrap().ContinueWith(t => {
                    if (t.Exception != null)
                    {
                        Log.Error(t.Exception);
                        Chat.AddMessage($"Couldn't connect to Twitch: {t.Exception.Message}");
                        DumpAssemblies();
                    }
                });
        }

        private void GameNetworkManager_onStopHostGlobal()
        {
            twitchManager.Disconnect().ContinueWith((t) =>
            {
                if (t.Exception != null)
                {
                    Log.Error(t.Exception);
                }
            });

            try
            {
                tiltifyManager.Disconnect();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private Task TwitchManager_OnMessageReceived(object sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {
            try
            {
                if (!NetworkServer.active)
                {
                    Log.Warning("[Server] Server not active");
                    return Task.CompletedTask;
                }

                string msg = e.ChatMessage.Message.Trim();
                // For numbers, we just care about the first word in the message
                string[] msgParts = msg.Split(SPACE, 2);
                if (int.TryParse(msgParts[0], out int index))
                {
                    Log.Info($"Vote added: {e.ChatMessage.Username} ({e.ChatMessage.UserId}) -> {index}");
                    itemRollerManager.AddVote(e.ChatMessage.UserId, index);
                }

                if (e.ChatMessage.Bits > 0 && configuration.EnableBitEvents.Value)
                {
                    RecievedBits(e.ChatMessage.DisplayName, e.ChatMessage.Bits);
                }

                if (e.ChatMessage.IsMe || e.ChatMessage.UserDetail.IsModerator || e.ChatMessage.IsBroadcaster)
                {
                    if (!eventDirector || !eventFactory)
                    {
                        return Task.CompletedTask;
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
                Log.Error(ex);
            }
            return Task.CompletedTask;
        }

        private void GiveRustedKey(string name)
        {
            eventDirector.AddEvent(eventFactory.BroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>{Util.EscapeRichTextForTextMeshPro(name)} wants you to take their rusted key.</color>"
            }));
            eventDirector.AddEvent(eventFactory.SpawnItem(PickupCatalog.FindPickupIndex(RoR2Content.Items.TreasureCache.itemIndex)));
        }

        private Task TwitchManager_OnRewardRedeemed(object sender, ChannelPointsCustomRewardRedemptionAddMessage e)
        {
            try
            {
                if (!configuration.ChannelPointsEnable.Value)
                {
                    Log.Warning($"Channel points disabled - Could not trigger event for Channel Points Redemption: {e.Reward.Title}");
                    return Task.CompletedTask;
                }
                if (channelPointsManager != null)
                {
                    bool triggered = channelPointsManager.TriggerEvent(e);
                    Log.Info($"Channel Points Redemption: {e.Reward.Title} (Event triggered: {triggered})");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            return Task.CompletedTask;
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
            choices.AddChoice(eventFactory.CreateBitStorm(), Math.Max(0, configuration.BitStormWeight.Value));
            choices.AddChoice(eventFactory.CreateBounty(), Math.Max(0, configuration.BountyWeight.Value));
            choices.AddChoice(eventFactory.TriggerShrineOfOrder(), Math.Max(0, configuration.ShrineOfOrderWeight.Value));
            // Make Shrine of the Mountain a bit more difficult compared to Channel Points
            choices.AddChoice(eventFactory.TriggerShrineOfTheMountain(2), Math.Max(0, configuration.ShrineOfTheMountainWeight.Value));
            choices.AddChoice(eventFactory.CreateMonster(MonsterSpawner.Monsters.TitanGold), Math.Max(0, configuration.TitanWeight.Value));
            choices.AddChoice(eventFactory.CreateMonster(MonsterSpawner.Monsters.LunarWisp, 2), Math.Max(0, configuration.LunarWispWeight.Value));
            choices.AddChoice(eventFactory.CreateMonster(MonsterSpawner.Monsters.Brother), Math.Max(0, configuration.MithrixWeight.Value));
            choices.AddChoice(eventFactory.CreateMonster(MonsterSpawner.Monsters.LemurianBruiser, RollForElite()), Math.Max(0, configuration.ElderLemurianWeight.Value));
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
                Log.Error(ex);
            }
        }

        private void BitsManager_OnUpdateBits(object sender, UpdateBitsEvent e)
        {
            configuration.CurrentBits.Value = e.Bits;

            BitsManager bitsManager = (BitsManager)sender;
            Log.Info($"Recieved bits: {e.Bits:N0} / {bitsManager.BitGoal:N0}");
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

            Log.Info($"Recieved donation; rolling Bit Reward");
            RollBitReward();
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

        private void HealthComponent_Suicide(On.RoR2.HealthComponent.orig_Suicide orig, HealthComponent self, GameObject killerOverride, GameObject inflictorOverride, DamageTypeCombo damageType)
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
                Log.Info($"Prevented suicide of {self.body.master}");
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
                    Log.Warning($"Starting vote for {string.Join(", ", vote.GetCandidates().Values)} with id {vote.GetId()}");
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
                    if (configuration.PublishToChat.Value)
                    {
                        twitchManager.SendMessage($"Chest opened! {string.Join(" | ", itemsString)}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
                var rollMessage = $"Choices: {String.Join(" | ", inGameItemsString)}";
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = rollMessage });

                // Show notification to local user
                var gameObject = LocalUserManager.GetFirstLocalUser().cachedMasterObject;
                if (configuration.SimpleUI.Value)
                {
                    VoteItems voteItems = gameObject.AddComponent<VoteItems>();
                    voteItems.SetItems(items, configuration.VoteDurationSec.Value);
                }
                else
                {
                    foreach (var item in e)
                    {
                        VoteItems voteItems = gameObject.AddComponent<VoteItems>();
                        voteItems.SetItems(items, configuration.VoteDurationSec.Value, item.Key);
                        voteItems.SetPosition(new Vector3(Screen.width * (640f / 1920f) , Screen.height / 2, 0) + new Vector3(0, ((item.Key - 1) * -128) + 128, 0));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            // FIMXE: Make RollManager a MonoBeheviour and let it manage the ending of votes.
            StartCoroutine(WaitToEndVote());
        }

        private IEnumerator WaitToEndVote()
        {
            yield return new WaitForSeconds(configuration.VoteDurationSec.Value);
            itemRollerManager.EndVote();
        }

        #region "Game Behaviour Changes"
        private void MultiShopController_OnPurchase(On.RoR2.MultiShopController.orig_OnPurchase orig, MultiShopController self, CostTypeDef.PayCostContext payCostContext, CostTypeDef.PayCostResults payCostResult)
        {
            if (!IsRunning())
            {
                orig(self, payCostContext, payCostResult);
                return;
            }

            if (!configuration.EnableItemVoting.Value)
            {
                orig(self, payCostContext, payCostResult);
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
                    PickupIndex dropPickupValue = shopTerminalBehavior.CurrentPickup().pickupIndex;
                    if (dropPickupValue != PickupIndex.none)
                    {
                        indices.Add(dropPickupValue);
                    }
                }

                itemRollerManager.RollForItem(indices, pickupIndex =>
                {
                    try
                    {
                        if (configuration.PublishToChat.Value)
                        {
                            string name = Language.GetString(PickupCatalog.GetPickupDef(pickupIndex).nameToken);
                            twitchManager.SendMessage($"Item picked: {name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                    if (eventDirector && eventFactory)
                    {
                        eventDirector.AddEvent(eventFactory.SpawnItem(pickupIndex));
                    }
                });
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            orig(self, payCostContext, payCostResult);
        }

        private void ShopTerminalBehavior_DropPickup(On.RoR2.ShopTerminalBehavior.orig_DropPickup orig, ShopTerminalBehavior self)
        {
            if (!NetworkServer.active)
            {
                orig(self);
                return;
            }

            if (!configuration.EnableItemVoting.Value)
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

                if (!configuration.EnableItemVoting.Value)
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

                if (!configuration.EnableChoosingLunarItems.Value && chestName.StartsWith("lunarchest"))
                {
                    orig(self);
                    return;
                }

                var dropPickupValue = self.currentPickup.pickupIndex;
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
                            if (configuration.PublishToChat.Value)
                            {
                                string name = Language.GetString(PickupCatalog.GetPickupDef(pickupIndex).nameToken);
                                twitchManager.SendMessage($"Item picked: {name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex);
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
                Log.Error(e);
            }
        }

        private PickupIndex[] RollVoteChest(ChestBehavior self, int maxDrops)
        {
            FieldInfo rngField = self.GetType().GetField("rng", BindingFlags.Instance | BindingFlags.NonPublic);
            Xoroshiro128Plus rng = (Xoroshiro128Plus)rngField.GetValue(self);
            if (configuration.ForceUniqueRolls.Value)
            {
                List<UniquePickup> pickups = new List<UniquePickup>();
                self.dropTable.GenerateDistinctPickups(pickups, maxDrops, rng);
                return pickups.ConvertAll((e) => e.pickupIndex).ToArray();
            }
            else
            {
                PickupIndex[] pickups = new PickupIndex[maxDrops];
                for (int i = 0; i < maxDrops; i++)
                {
                    pickups[i] = self.dropTable.GeneratePickup(rng).pickupIndex;
                }
                return pickups;
            }
        }
        #endregion
    }
}
