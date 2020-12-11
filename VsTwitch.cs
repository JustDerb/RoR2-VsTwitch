using BepInEx;
using BepInEx.Configuration;
using R2API.Utils;
using RoR2;
using RoR2.Networking;
using RoR2.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace VsTwitch
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(GUID, ModName, Version)]
    [R2APISubmoduleDependency(nameof(CommandHelper))]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync)]
    public class VsTwitch : BaseUnityPlugin
    {
        private static readonly char[] SPACE = new char[] { ' ' };
        public const string GUID = "com.justinderby.vstwitch";
        public const string ModName = "Vs Twitch";
        public const string Version = "1.0.1";

        // This is only used for ConCommands, since they need to be static...
        public static VsTwitch Instance;

        private BitsManager bitsManager;
        private TwitchManager twitchManager;
        private ItemRollerManager itemRollerManager;
        private LanguageOverride languageOverride;
        private EventDirector eventDirector;
        private EventFactory eventFactory;

        // Twitch
        public static ConfigEntry<string> TwitchChannel { get; set; }
        public static ConfigEntry<string> TwitchUsername { get; set; }
        public static ConfigEntry<string> TwitchOAuth { get; set; }
        public static ConfigEntry<bool> EnableItemVoting { get; set; }
        public static ConfigEntry<int> VoteDurationSec { get; set; }
        public static ConfigEntry<bool> EnableBitEvents { get; set; }
        public static ConfigEntry<int> BitsThreshold { get; set; }
        public static ConfigEntry<int> CurrentBits { get; set; }

        // Event
        public static ConfigEntry<int> BitStormWeight { get; set; }
        public static ConfigEntry<int> BountyWeight { get; set; }
        public static ConfigEntry<int> TitanWeight { get; set; }
        public static ConfigEntry<int> LunarWispWeight { get; set; }
        public static ConfigEntry<int> MithrixWeight { get; set; }
        public static ConfigEntry<int> ElderLemurianWeight { get; set; }

        // UI
        public static ConfigEntry<bool> SimpleUI { get; set; }

        #region "Constructors/Destructors"
        public void Awake()
        {
            Instance = SingletonHelper.Assign(Instance, this);

            CommandHelper.AddToConsoleWhenReady();

            // Twitch
            TwitchChannel = Config.Bind("Twitch", "Channel", "", "Your Twitch channel name");
            TwitchUsername = Config.Bind("Twitch", "Username", "", "Your Twitch username");
            TwitchOAuth = Config.Bind("Twitch", "ImplicitOAuth", "", "Implicite OAuth code (this is not your password - it's a generated password!): " +
                "https://id.twitch.tv/oauth2/authorize?response_type=token&client_id=q6batx0epp608isickayubi39itsckt&redirect_uri=https://twitchapps.com/tmi/&scope=channel_subscriptions+user_subscriptions+channel_check_subscription+bits:read+chat:read+chat:edit+channel:read:redemptions+channel:read:hype_train");
            EnableItemVoting = Config.Bind("Twitch", "EnableItemVoting", true, "Enable item voting on Twitch.");
            VoteDurationSec = Config.Bind("Twitch", "VoteDurationdSec", 20, "How long to allow twitch voting.");
            EnableBitEvents = Config.Bind("Twitch", "EnableBitEvents", true, "Enable bit events from Twitch.");
            BitsThreshold = Config.Bind("Twitch", "BitsThreshold", 1500, "How many Bits are needed before something happens.");
            CurrentBits = Config.Bind("Twitch", "CurrentBits", 0, "(DO NOT EDIT) How many Bits have currently been donated.");
            // Event
            BitStormWeight = Config.Bind("Event", "BitStormWeight", 1, "Weight for the bit storm bit event. Set to 0 to disable.");
            BountyWeight = Config.Bind("Event", "BountyWeight", 1, "Weight for the doppleganger bit event. Set to 0 to disable.");
            TitanWeight = Config.Bind("Event", "TitanWeight", 1, "Weight for the Aurelionite bit event. Set to 0 to disable.");
            LunarWispWeight = Config.Bind("Event", "LunarWispWeight", 1, "Weight for the Lunar Chimera (Wisp) bit event. Set to 0 to disable.");
            MithrixWeight = Config.Bind("Event", "MithrixWeight", 1, "Weight for the Mithrix bit event. Set to 0 to disable.");
            ElderLemurianWeight = Config.Bind("Event", "ElderLemurianWeight", 1, "Weight for the Elder Lemurian bit event. Set to 0 to disable.");
            // UI
            SimpleUI = Config.Bind("UI", "SimpleUI", false, "Simplify the UI. Set to true if you are playing Multiplayer.");

            bitsManager = new BitsManager(CurrentBits.Value);
            twitchManager = new TwitchManager();
            itemRollerManager = new ItemRollerManager(new MaxVoteStrategy<PickupIndex>());
            languageOverride = new LanguageOverride
            {
                StreamerName = TwitchChannel.Value
            };

            GameNetworkManager.onStartHostGlobal += GameNetworkManager_onStartHostGlobal;
            GameNetworkManager.onStopHostGlobal += GameNetworkManager_onStopHostGlobal;
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
        }

        public void OnDestroy()
        {
            Instance = SingletonHelper.Unassign(Instance, this);

            GameNetworkManager.onStartHostGlobal -= GameNetworkManager_onStartHostGlobal;
            GameNetworkManager.onStopHostGlobal -= GameNetworkManager_onStopHostGlobal;
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

            On.RoR2.ChestBehavior.ItemDrop += ChestBehavior_ItemDrop;
            On.RoR2.ShopTerminalBehavior.DropPickup += ShopTerminalBehavior_DropPickup;
            On.RoR2.MultiShopController.DisableAllTerminals += MultiShopController_DisableAllTerminals;

            if (self.gameObject.GetComponent<EventDirector>() == null)
            {
                eventDirector = self.gameObject.AddComponent<EventDirector>();
                eventDirector.OnProcessingEventsChanged += (sender, processing) =>
                {
                    var message = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>VsTwitch:</color> Events {(processing ? "enabled" : "paused")}.";
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = message });
                };
            }
            if (self.gameObject.GetComponent<EventFactory>() == null)
            {
                eventFactory = self.gameObject.AddComponent<EventFactory>();
            }
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
            On.RoR2.MultiShopController.DisableAllTerminals -= MultiShopController_DisableAllTerminals;

            if (eventDirector)
            {
                Destroy(eventDirector);
                eventDirector = null;
            }
            if (eventFactory)
            {
                Destroy(eventFactory);
                eventFactory = null;
            }
        }
        #endregion

        #region "Twitch Integration"
        [ConCommand(commandName = "vs_connect_twitch", flags = ConVarFlags.SenderMustBeServer, helpText = "Connect to Twitch.")]
        private static void ConnectToTwitch(ConCommandArgs args)
        {
            if (!Instance)
            {
                Debug.LogError("VsTwitch mod not instatiated!");
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
                Instance.twitchManager.Connect(channel, oauthToken, username);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [ConCommand(commandName = "vs_add_bits", flags = ConVarFlags.SenderMustBeServer, helpText = "Fake add bits.")]
        private static void TwitchAddBits(ConCommandArgs args)
        {
            if (!Instance)
            {
                Debug.LogError("VsTwitch mod not instatiated!");
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
        private static void TwitchSetBitGoal(ConCommandArgs args)
        {
            if (!Instance)
            {
                Debug.LogError("VsTwitch mod not instatiated!");
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
            return twitchManager != null && twitchManager.IsConnected();
        }

        private void GameNetworkManager_onStartHostGlobal()
        {
            try
            {
                twitchManager.Connect(TwitchChannel.Value, TwitchOAuth.Value, TwitchUsername.Value);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Chat.AddMessage($"Couldn't connect to Twitch: {e.Message}");
                if (e is ArgumentException)
                {
                    Chat.AddMessage($"Did you configure the mod correctly?");
                }
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
                        case "!roll":
                            RollBitReward();
                            break;
                        case "!meteor":
                            eventDirector.AddEvent(eventFactory.CreateBitStorm());
                            break;
                        case "!bounty":
                            eventDirector.AddEvent(eventFactory.CreateBounty());
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
            choices.AddChoice(eventFactory.CreateMonster(MonsterSpawner.Monsters.TitanGold), Math.Max(0, TitanWeight.Value));
            choices.AddChoice(eventFactory.CreateMonster(MonsterSpawner.Monsters.LunarWisp, 2), Math.Max(0, LunarWispWeight.Value));
            choices.AddChoice(eventFactory.CreateMonster(MonsterSpawner.Monsters.Brother), Math.Max(0, MithrixWeight.Value));
            choices.AddChoice(eventFactory.CreateMonster(MonsterSpawner.Monsters.LemurianBruiser, RollForElite()), Math.Max(0, ElderLemurianWeight.Value));
            eventDirector.AddEvent(choices.Evaluate(UnityEngine.Random.value));
        }

        private EliteIndex RollForElite(bool forceElite = false)
        {
            Array choices = Enum.GetValues(typeof(EliteIndex));
            EliteIndex choice;
            do
            {
                choice = (EliteIndex)choices.GetValue(UnityEngine.Random.Range(0, choices.Length));
                // None is weighted twice here, but that's okay
                choice = choice != EliteIndex.Count ? choice : EliteIndex.None;
            } while (forceElite && choice == EliteIndex.None);

            return choice;
        }

        private void RecievedBits(string username, int bits)
        {
            try
            {
                var rollMessage = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>" +
                    $"{Util.EscapeRichTextForTextMeshPro(username)} throws bits into the pool. Twitch Chat's temptation grows...</color> " +
                    $"({bitsManager.Bits + bits}/{bitsManager.BitGoal})";
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
            Debug.Log($"Recieved bits: {e.Bits} / {bitsManager.BitGoal}");
            // FIXME: Add credits to spawn director
            if (e.Bits >= bitsManager.BitGoal)
            {
                // FIXME: Can cause infinite loop (as ResetBits causes this event to fire again)
                bitsManager.ResetBits(true);
                RollBitReward();
            }
        }
        #endregion

        #region "Localization Overrides"
        private string Language_GetLocalizedStringByToken(On.RoR2.Language.orig_GetLocalizedStringByToken orig, Language self, string token)
        {
            if (languageOverride.TryGetLocalizedStringByToken(token, out string result))
            {
                return result;
            }
            return orig(self, token);
        }
        #endregion

        private void ItemRollerManager_OnVoteStart(object sender, IDictionary<int, PickupIndex> e)
        {
            try
            {
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
                twitchManager.SendMessage($"Chest opened! {String.Join(" | ", itemsString)}");
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
                        voteItems.SetPosition(new Vector3(256 + 32, Screen.height / 2, 0) + new Vector3(0, ((item.Key - 1) * -128) + 128, 0));
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
        private void MultiShopController_DisableAllTerminals(On.RoR2.MultiShopController.orig_DisableAllTerminals orig, MultiShopController self, Interactor interactor)
        {
            if (!IsRunning())
            {
                orig(self, interactor);
                return;
            }

            if (!EnableItemVoting.Value)
            {
                orig(self, interactor);
                return;
            }

            try {
                // Grab and add all pickups at terminal
                List<PickupIndex> indices = new List<PickupIndex>();

                FieldInfo terminalGameObjects = self.GetType().GetField("terminalGameObjects", BindingFlags.Instance | BindingFlags.NonPublic);
                GameObject[] terminalGameObjectsValue = (GameObject[])terminalGameObjects.GetValue(self);
                foreach (GameObject gameObject in terminalGameObjectsValue)
                {
                    ShopTerminalBehavior shopTerminalBehavior = gameObject.GetComponent<ShopTerminalBehavior>();
                    FieldInfo dropPickup = shopTerminalBehavior.GetType().GetField("pickupIndex", BindingFlags.Instance | BindingFlags.NonPublic);
                    PickupIndex dropPickupValue = (PickupIndex)dropPickup.GetValue(shopTerminalBehavior);
                    if (dropPickupValue != PickupIndex.none)
                    {
                        indices.Add(dropPickupValue);
                    }
                }

                itemRollerManager.RollForItem(indices, pickupIndex =>
                {
                    string name = Language.GetString(PickupCatalog.GetPickupDef(pickupIndex).nameToken);
                    twitchManager.SendMessage($"Item picked: {name}");
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

            orig(self, interactor);
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
            MethodInfo setHasBeenPurchased = self.GetType().GetMethod("SetHasBeenPurchased", BindingFlags.Instance | BindingFlags.NonPublic);
            setHasBeenPurchased.Invoke(self, new object[] { true });

            // We purposefully don't call the original because then it'd create the pickup drop :(
            // orig(self);
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

                if (chestName.StartsWith("lunarchest"))
                {
                    orig(self);
                    return;
                }

                FieldInfo dropPickup = self.GetType().GetField("dropPickup", BindingFlags.Instance | BindingFlags.NonPublic);
                var dropPickupValue = (PickupIndex)dropPickup.GetValue(self);
                if (dropPickupValue != PickupIndex.none)
                {
                    var itemdef = PickupCatalog.GetPickupDef(dropPickupValue);
                    List<PickupIndex> indices = new List<PickupIndex>();
                    if (itemdef.equipmentIndex != EquipmentIndex.None)
                    {
                        indices.Add(dropPickupValue);
                        indices.Add(RollVoteEquipment());
                        indices.Add(RollVoteEquipment());
                    }
                    else
                    {
                        indices.Add(dropPickupValue);
                        indices.Add(RollVoteItem(self));
                        indices.Add(RollVoteItem(self));
                    }

                    itemRollerManager.RollForItem(indices, pickupIndex =>
                    {
                        string name = Language.GetString(PickupCatalog.GetPickupDef(pickupIndex).nameToken);
                        twitchManager.SendMessage($"Item picked: {name}");
                        if (eventDirector && eventFactory)
                        {
                            eventDirector.AddEvent(eventFactory.SpawnItem(pickupIndex));
                        }
                    });

                    // Clear the item so we don't spawn anything from the object
                    dropPickup.SetValue(self, PickupIndex.none);
                }
                orig(self);
            } catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private PickupIndex RollVoteEquipment()
        {
            List<PickupIndex> equipmentList = Run.instance.availableEquipmentDropList;

            return equipmentList[UnityEngine.Random.Range(0, equipmentList.Count)];
        }

        private PickupIndex RollVoteItem(ChestBehavior self)
        {
            WeightedSelection<List<PickupIndex>> WeightedSelection = new WeightedSelection<List<PickupIndex>>();
            WeightedSelection.AddChoice(Run.instance.availableTier1DropList, self.tier1Chance);
            WeightedSelection.AddChoice(Run.instance.availableTier2DropList, self.tier2Chance);
            WeightedSelection.AddChoice(Run.instance.availableTier3DropList, self.tier3Chance);
            WeightedSelection.AddChoice(Run.instance.availableLunarDropList, self.lunarChance);

            List<PickupIndex> DropList = WeightedSelection.Evaluate(UnityEngine.Random.value);

            return DropList[UnityEngine.Random.Range(0, DropList.Count)];
        }
        #endregion
    }
}
