using RoR2;
using RoR2.Artifacts;
using RoR2.CharacterAI;
using System;
using System.Collections;
using System.Reflection;
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
            if (!spawner)
            {
                spawner = gameObject.AddComponent<MonsterSpawner>();
            }
        }

        public Func<EventDirector, IEnumerator> BroadcastChat(ChatMessageBase message)
        {
            return (director) => { return BroadcastChatInternal(message); };
        }

        private IEnumerator BroadcastChatInternal(ChatMessageBase message)
        {
            Chat.SendBroadcastChat(message);
            yield break;
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
                if (RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Command))
                {
                    // Sorry, you're forced to pick up the equipment :(
                    ItemManager.GiveToAllPlayers(pickupIndex);
                }
                else
                {
                    ItemManager.DropToAllPlayers(pickupIndex, Vector3.up * 10f);
                }
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
                    MeteorStormController component = Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/MeteorStorm"),
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

        public Func<EventDirector, IEnumerator> TriggerShrineOfOrder()
        {
            return (director) => { return TriggerShrineOfOrderInternal(); };
        }

        private IEnumerator TriggerShrineOfOrderInternal()
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

                    Inventory inventory = controller.master.inventory;
                    if (inventory)
                    {
                        inventory.ShrineRestackInventory(RoR2Application.rng);

                        CharacterBody body = controller.master.GetBody();
                        if (body)
                        {
                            Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                            {
                                subjectAsCharacterBody = body,
                                baseToken = "SHRINE_RESTACK_USE_MESSAGE"
                            });
                            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
                            {
                                origin = body.corePosition,
                                rotation = Quaternion.identity,
                                scale = body.bestFitRadius * 2f,
                                color = new Color(1f, 0.23f, 0.6337214f)
                            }, true);
                        }
                    }
                }

                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>Twitch Chat decides you should have 'better' items.</color>"
                });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            yield break;
        }

        public Func<EventDirector, IEnumerator> TriggerShrineOfTheMountain(int count = 1)
        {
            return (director) => { return TriggerShrineOfTheMountainInternal(count); };
        }

        private IEnumerator TriggerShrineOfTheMountainInternal(int count)
        {
            // For the last stages let's do something slightly different
            if (SceneCatalog.GetSceneDefForCurrentScene().isFinalStage)
            {
                // Give every enemy that's currently out an extra life and a couple extra equipment
                foreach (var teamComponent in TeamComponent.GetTeamMembers(TeamIndex.Monster))
                {
                    CharacterBody body = teamComponent.body;
                    if (body)
                    {
                        if (body.inventory.GetItemCount(RoR2Content.Items.ExtraLife) == 0 &&
                            body.inventory.GetItemCount(RoR2Content.Items.ExtraLifeConsumed) == 0)
                        {
                            body.inventory.GiveItem(RoR2Content.Items.ExtraLife, 1);
                            body.inventory.GiveRandomItems(Run.instance.livingPlayerCount, false, true);
                        }
                    }
                }

                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>Twitch Chat feels the last stage should be harder.</color>"
                });
                yield break;
            }

            // Wait until the teleporter isn't active
            if (TeleporterInteraction.instance && !TeleporterInteraction.instance.isIdle)
            {
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>Twitch Chat feels the boss should be harder on the next stage.</color>"
                });
            }
            yield return new WaitUntil(() => {
                return TeleporterInteraction.instance && TeleporterInteraction.instance.isIdle;
            });

            try
            {
                if (TeleporterInteraction.instance)
                {
                    for (int i = 0; i < count; ++i)
                    {
                        TeleporterInteraction.instance.AddShrineStack();
                    }

                    foreach (var controller in PlayerCharacterMasterController.instances)
                    {
                        CharacterBody body = controller.master.GetBody();
                        if (body)
                        {
                            Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                            {
                                subjectAsCharacterBody = body,
                                baseToken = "SHRINE_BOSS_USE_MESSAGE"
                            });
                            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
                            {
                                origin = body.corePosition,
                                rotation = Quaternion.identity,
                                scale = body.bestFitRadius * 2f,
                                color = new Color(0.7372549f, 0.90588236f, 0.94509804f)
                            }, true);
                        }
                    }
                }

                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = $"<color=#{TwitchConstants.TWITCH_COLOR_MAIN}>Twitch Chat feels the boss should be harder on this stage.</color>"
                });
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
            string characterName = null;
            foreach (var member in squad.readOnlyMembersList)
            {
                if (characterName == null) {
                    characterName = Util.GetBestBodyName(member.GetBodyObject());
                }
                SpawnedMonster spawnedMonster = member.gameObject.AddComponent<SpawnedMonster>();
                spawnedMonster.teleportWhenOOB = true;
                CharacterBody body = member.GetBody();
                if (body)
                {
                    body.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
                }
            }
            if (characterName == null)
            {
                characterName = "monster";
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
                    string[] standardDeathQuoteTokens = (string[])typeof(GlobalEventManager)
                        .GetField("standardDeathQuoteTokens", BindingFlags.Static | BindingFlags.NonPublic)
                        .GetValue(null);
                    Chat.SendBroadcastChat(new Chat.PlayerDeathChatMessage
                    {
                        subjectAsCharacterBody = member.GetBody(),
                        baseToken = standardDeathQuoteTokens[UnityEngine.Random.Range(0, standardDeathQuoteTokens.Length)]
                    });
                }
            };

            foreach (var member in squad.readOnlyMembersList)
            {
                // This is the magic sauce to keep the ally around for the next stages
                member.gameObject.AddComponent<SetDontDestroyOnLoad>();
                // This is to make the name "stick" between stages
                ForceNameChange nameChange = member.gameObject.AddComponent<ForceNameChange>();
                nameChange.NameToken = nameEscaped;

                SpawnedMonster spawnedMonster = member.gameObject.AddComponent<SpawnedMonster>();
                // Allies aren't special here, let the regular game code handle them.
                spawnedMonster.teleportWhenOOB = false;

                // Help them out since the AI are pretty bad at staying alive
                member.inventory.GiveItem(RoR2Content.Items.HealWhileSafe, Run.instance.livingPlayerCount);
                member.inventory.GiveItem(RoR2Content.Items.Infusion, Run.instance.livingPlayerCount);

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

                member.inventory.GiveItem(RoR2Content.Items.Hoof, summonerMaster.inventory.GetItemCount(RoR2Content.Items.Hoof));
                member.inventory.GiveItem(RoR2Content.Items.SprintBonus, summonerMaster.inventory.GetItemCount(RoR2Content.Items.SprintBonus));

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
