using RoR2;
using RoR2.Artifacts;
using RoR2.CharacterAI;
using RoR2.UI;
using System;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace VsTwitch
{
    /// <summary>
    /// Factory class for creating common events for the <c>EventDirector</c>.
    /// </summary>
    [RequireComponent(typeof(MonsterSpawner))]
    class EventFactory : MonoBehaviour
    {
        private MonsterSpawner spawner;

        public void Awake()
        {
            spawner = GetComponent<MonsterSpawner>();
            if (!spawner)
            {
                spawner = gameObject.AddComponent<MonsterSpawner>();
            }
        }

        public Func<EventDirector, IEnumerator> SpawnItem(PickupIndex pickupIndex)
        {
            return (director) => { return SpawnItemInternal(pickupIndex); };
        }

        private IEnumerator SpawnItemInternal(PickupIndex pickupIndex)
        {
            var itemdef = PickupCatalog.GetPickupDef(pickupIndex);
            if (itemdef.equipmentIndex != EquipmentIndex.None)
            {
                ItemManager.DropToAllPlayers(pickupIndex, Vector3.up * 10f);
            }
            else
            {
                ItemManager.GiveToAllPlayers(pickupIndex);
            }
            yield break;
        }

        public Func<EventDirector, IEnumerator> CreateBounty()
        {
            return (director) => { return CreateBountyInternal(); };
        }

        private IEnumerator CreateBountyInternal()
        {
            try
            {
                //for (int i = CharacterMaster.readOnlyInstancesList.Count - 1; i >= 0; i--)
                //{
                //    CharacterMaster characterMaster = CharacterMaster.readOnlyInstancesList[i];
                //    if (characterMaster.teamIndex == TeamIndex.Player && characterMaster.playerCharacterMasterController)
                //    {
                //        DoppelgangerInvasionManager.CreateDoppelganger(characterMaster, rng);
                //    }
                //}
                DoppelgangerInvasionManager.PerformInvasion(new Xoroshiro128Plus((ulong)UnityEngine.Random.Range(0f, 10000f)));

                var rollMessage = $"<color=#{ColorUtility.ToHtmlStringRGB(Color.red)}><size=120%>HOLD ON TO YER BOOTY!!</size></color>";
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = rollMessage });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            yield break;
        }

        public Func<EventDirector, IEnumerator> CreateBitStorm()
        {
            return (director) => { return CreateBitStormInternal(); };
        }

        private IEnumerator CreateBitStormInternal()
        {
            try
            {
                foreach (var controller in PlayerCharacterMasterController.instances)
                {
                    // Skip dead players
                    if (controller.master.IsDeadAndOutOfLivesServer())
                    {
                        continue;
                    }

                    CharacterBody body = controller.master.GetBody();
                    MeteorStormController component = Instantiate(Resources.Load<GameObject>("Prefabs/NetworkedObjects/MeteorStorm"),
                        body.corePosition, Quaternion.identity).GetComponent<MeteorStormController>();
                    component.owner = base.gameObject;
                    component.ownerDamage = body.damage * 2f;
                    component.isCrit = Util.CheckRoll(body.crit, body.master);
                    // Increase the length
                    component.waveCount *= 3;
                    NetworkServer.Spawn(component.gameObject);
                }

                var rollMessage = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}><size=150%>HERE COMES THE BITS!!</size></color>";
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = rollMessage });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            yield break;
        }

        public Func<EventDirector, IEnumerator> CreateMonster(string monster, EliteIndex eliteIndex)
        {
            return CreateMonster(monster, 1, eliteIndex);
        }

        public Func<EventDirector, IEnumerator> CreateMonster(string monster, int num = 1, EliteIndex eliteIndex = EliteIndex.None)
        {
            return (director) => { return CreateMonsterInternal(director, monster, num, eliteIndex); };
        }

        private IEnumerator CreateMonsterInternal(EventDirector director, string monster, int num, EliteIndex eliteIndex)
        {
            CombatSquad squad = spawner.SpawnMonsters(monster, eliteIndex, FindFirstAliveBody(), num, director);
            var characterName = "monster";
            foreach (var member in squad.readOnlyMembersList)
            {
                characterName = Util.GetBestBodyName(member.GetBodyObject());
                break;
            }
            // FIXME: Use "{0} has summoned {1}" text
            var pluralize = squad.readOnlyMembersList.Count == 1 ? $"{characterName} jumps" : $"{squad.readOnlyMembersList.Count} {characterName}'s jump";
            var message = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>{pluralize} out of the void...</color>";
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = message });

            if (MonsterSpawner.Monsters.Brother.Equals(monster) || MonsterSpawner.Monsters.BrotherGlass.Equals(monster))
            {
                squad.onMemberLost += (member) =>
                {
                    if (squad.memberCount == 0)
                    {
                        Chat.SendBroadcastChat(new Chat.SimpleChatMessage {
                            baseToken = "BROTHER_DIALOGUE_FORMAT",
                            paramTokens = new string[] { "I'll be back" },
                        });
                    }
                };
            }
            yield break;
        }

        public Func<EventDirector, IEnumerator> CreateAlly(string name, string monster, EliteIndex eliteIndex = EliteIndex.None)
        {
            return (director) => { return CreateAllyInternal(name, monster, eliteIndex); };
        }

        private IEnumerator CreateAllyInternal(string name, string monster, EliteIndex eliteIndex)
        {
            string nameEscaped = Util.EscapeRichTextForTextMeshPro(name);

            CharacterBody summonerBody = FindFirstAliveBody();
            CombatSquad squad = spawner.SpawnAllies(monster, eliteIndex, summonerBody);
            var message = $"<color=#{ColorUtility.ToHtmlStringRGB(Color.green)}>{nameEscaped}</color>" +
                $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}> enters the game to help you...</color>";
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = message });

            squad.onMemberLost += (member) =>
            {
                if (squad.memberCount == 0)
                {
                    var deathMessage = $"<color=#{ColorUtility.ToHtmlStringRGB(Color.green)}>{nameEscaped}</color>" +
                        $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}> has died! :(</color>";
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = deathMessage });
                }
            };

            foreach (var member in squad.readOnlyMembersList)
            {
                // This is the magic sauce to keep the ally around for the next stages
                member.gameObject.AddComponent<SetDontDestroyOnLoad>();
                // This is to make the name "stick" between stages
                ForceNameChange nameChange = member.gameObject.AddComponent<ForceNameChange>();
                nameChange.NameToken = nameEscaped;

                // Help them out since the AI are pretty bad at staying alive
                member.inventory.GiveItem(ItemIndex.HealOnCrit, Run.instance.livingPlayerCount);
                member.inventory.GiveItem(ItemIndex.HealWhileSafe, Run.instance.livingPlayerCount);
                member.inventory.GiveItem(ItemIndex.Medkit, Run.instance.livingPlayerCount);
                member.inventory.GiveItem(ItemIndex.Infusion, Run.instance.livingPlayerCount);
                member.inventory.GiveItem(ItemIndex.Hoof, member.inventory.GetItemCount(ItemIndex.Hoof));
                member.inventory.GiveItem(ItemIndex.SprintBonus, member.inventory.GetItemCount(ItemIndex.SprintBonus));

                //CharacterBody memberBody = member.GetBody();
                //if (memberBody)
                //{
                //    Nameplate nameplate = memberBody.teamComponent.gameObject.AddComponent<Nameplate>();
                //    nameplate.label = nameplate.transform.gameObject.AddComponent<TextMeshPro>();
                //    nameplate.coloredSprites = new SpriteRenderer[0];
                //    nameplate.combatColor = Color.red;
                //    nameplate.baseColor = Color.magenta;
                //    nameplate.aliveObject = new GameObject();
                //    nameplate.deadObject = new GameObject();
                //    nameplate.SetBody(memberBody);
                //}

                if (summonerBody == null)
                {
                    continue;
                }

                CharacterMaster summonerMaster = summonerBody.master;
                if (summonerMaster && summonerMaster.minionOwnership.ownerMaster)
                {
                    summonerMaster = summonerMaster.minionOwnership.ownerMaster;
                }
                member.minionOwnership.SetOwner(summonerBody.master);

                AIOwnership aIOwnership = member.gameObject.GetComponent<AIOwnership>();
                if (aIOwnership)
                {
                    if (summonerBody.master)
                    {
                        aIOwnership.ownerMaster = summonerBody.master;
                    }
                }
                BaseAI baseAI = member.gameObject.GetComponent<BaseAI>();
                if (baseAI)
                {
                    baseAI.leader.gameObject = summonerBody.gameObject;
                    baseAI.fullVision = true;
                    ApplyFollowingAI(baseAI);
                }
            }

            yield break;
        }

        private void ApplyFollowingAI(BaseAI baseAI)
        {
            AISkillDriver returnToOwnerLeash = baseAI.gameObject.AddComponent<AISkillDriver>();
            returnToOwnerLeash.customName = "ReturnToOwnerLeash";
            returnToOwnerLeash.skillSlot = SkillSlot.None;
            returnToOwnerLeash.requiredSkill = null;
            returnToOwnerLeash.requireSkillReady = false;
            returnToOwnerLeash.requireEquipmentReady = false;
            returnToOwnerLeash.minUserHealthFraction = float.NegativeInfinity;
            returnToOwnerLeash.maxUserHealthFraction = float.PositiveInfinity;
            returnToOwnerLeash.minTargetHealthFraction = float.NegativeInfinity;
            returnToOwnerLeash.maxTargetHealthFraction = float.PositiveInfinity;
            returnToOwnerLeash.minDistance = 50f;
            returnToOwnerLeash.maxDistance = float.PositiveInfinity;
            returnToOwnerLeash.selectionRequiresTargetLoS = false;
            returnToOwnerLeash.selectionRequiresOnGround = false;
            returnToOwnerLeash.moveTargetType = AISkillDriver.TargetType.CurrentLeader;
            returnToOwnerLeash.activationRequiresTargetLoS = false;
            returnToOwnerLeash.activationRequiresAimConfirmation = false;
            returnToOwnerLeash.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            returnToOwnerLeash.moveInputScale = 1f;
            returnToOwnerLeash.aimType = AISkillDriver.AimType.AtCurrentLeader;
            returnToOwnerLeash.ignoreNodeGraph = false;
            returnToOwnerLeash.shouldSprint = true;
            returnToOwnerLeash.shouldFireEquipment = false;
            returnToOwnerLeash.buttonPressType = AISkillDriver.ButtonPressType.Hold;
            returnToOwnerLeash.driverUpdateTimerOverride = 3f;
            returnToOwnerLeash.resetCurrentEnemyOnNextDriverSelection = true;
            returnToOwnerLeash.noRepeat = false;
            returnToOwnerLeash.nextHighPriorityOverride = null;

            AISkillDriver returnToLeaderDefault = baseAI.gameObject.AddComponent<AISkillDriver>();
            returnToLeaderDefault.customName = "ReturnToLeaderDefault";
            returnToLeaderDefault.skillSlot = SkillSlot.None;
            returnToLeaderDefault.requiredSkill = null;
            returnToLeaderDefault.requireSkillReady = false;
            returnToLeaderDefault.requireEquipmentReady = false;
            returnToLeaderDefault.minUserHealthFraction = float.NegativeInfinity;
            returnToLeaderDefault.maxUserHealthFraction = float.PositiveInfinity;
            returnToLeaderDefault.minTargetHealthFraction = float.NegativeInfinity;
            returnToLeaderDefault.maxTargetHealthFraction = float.PositiveInfinity;
            returnToLeaderDefault.minDistance = 15f;
            returnToLeaderDefault.maxDistance = float.PositiveInfinity;
            returnToLeaderDefault.selectionRequiresTargetLoS = false;
            returnToLeaderDefault.selectionRequiresOnGround = false;
            returnToLeaderDefault.moveTargetType = AISkillDriver.TargetType.CurrentLeader;
            returnToLeaderDefault.activationRequiresTargetLoS = false;
            returnToLeaderDefault.activationRequiresAimConfirmation = false;
            returnToLeaderDefault.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            returnToLeaderDefault.moveInputScale = 1f;
            returnToLeaderDefault.aimType = AISkillDriver.AimType.AtMoveTarget;
            returnToLeaderDefault.ignoreNodeGraph = false;
            returnToLeaderDefault.shouldSprint = true;
            returnToLeaderDefault.shouldFireEquipment = false;
            returnToLeaderDefault.buttonPressType = AISkillDriver.ButtonPressType.Hold;
            returnToLeaderDefault.driverUpdateTimerOverride = -1f;
            returnToLeaderDefault.resetCurrentEnemyOnNextDriverSelection = false;
            returnToLeaderDefault.noRepeat = false;
            returnToLeaderDefault.nextHighPriorityOverride = null;

            AISkillDriver waitNearLeaderDefault = baseAI.gameObject.AddComponent<AISkillDriver>();
            waitNearLeaderDefault.customName = "WaitNearLeaderDefault";
            waitNearLeaderDefault.skillSlot = SkillSlot.None;
            waitNearLeaderDefault.requiredSkill = null;
            waitNearLeaderDefault.requireSkillReady = false;
            waitNearLeaderDefault.requireEquipmentReady = false;
            waitNearLeaderDefault.minUserHealthFraction = float.NegativeInfinity;
            waitNearLeaderDefault.maxUserHealthFraction = float.PositiveInfinity;
            waitNearLeaderDefault.minTargetHealthFraction = float.NegativeInfinity;
            waitNearLeaderDefault.maxTargetHealthFraction = float.PositiveInfinity;
            waitNearLeaderDefault.minDistance = 0f;
            waitNearLeaderDefault.maxDistance = float.PositiveInfinity;
            waitNearLeaderDefault.selectionRequiresTargetLoS = false;
            waitNearLeaderDefault.selectionRequiresOnGround = false;
            waitNearLeaderDefault.moveTargetType = AISkillDriver.TargetType.CurrentLeader;
            waitNearLeaderDefault.activationRequiresTargetLoS = false;
            waitNearLeaderDefault.activationRequiresAimConfirmation = false;
            waitNearLeaderDefault.movementType = AISkillDriver.MovementType.Stop;
            waitNearLeaderDefault.moveInputScale = 1f;
            waitNearLeaderDefault.aimType = AISkillDriver.AimType.AtCurrentLeader;
            waitNearLeaderDefault.ignoreNodeGraph = false;
            waitNearLeaderDefault.shouldSprint = false;
            waitNearLeaderDefault.shouldFireEquipment = false;
            waitNearLeaderDefault.buttonPressType = AISkillDriver.ButtonPressType.Hold;
            waitNearLeaderDefault.driverUpdateTimerOverride = -1f;
            waitNearLeaderDefault.resetCurrentEnemyOnNextDriverSelection = false;
            waitNearLeaderDefault.noRepeat = false;
            waitNearLeaderDefault.nextHighPriorityOverride = null;

            PropertyInfo skillDriversProperty = typeof(BaseAI).GetProperty("skillDrivers", BindingFlags.Instance | BindingFlags.Public);
            if (skillDriversProperty != null)
            {
                skillDriversProperty.SetValue(baseAI, baseAI.gameObject.GetComponents<AISkillDriver>(), null);
            }
        }

        private CharacterBody FindFirstAliveBody()
        {
            foreach (var controller in PlayerCharacterMasterController.instances)
            {
                var player = controller.master;
                var body = player.GetBody();
                if (body != null)
                {
                    return body;
                }
            }
            return null;
        }
    }
}
