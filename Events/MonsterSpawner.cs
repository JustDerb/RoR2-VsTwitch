using RoR2;
using System;
using UnityEngine;

namespace VsTwitch
{
    /// <summary>
    /// Spawns monsters, as enemies or as allies. Iterate the <c>CombatSquad</c> to update individual monsters (like giving them items).
    /// You can also listen to <c>CombatGroup.onMemberLost</c> and when the squad is down to 0 you know everything is dead in it.
    /// </summary>
    class MonsterSpawner : MonoBehaviour
    {
        public CombatSquad SpawnAllies(
            string spawnCard,
            EliteIndex eliteIndex = EliteIndex.None,
            CharacterBody targetBody = null,
            int numMonsters = 1)
        {
            return SpawnMonstersInternal(spawnCard, eliteIndex, targetBody, numMonsters, null, TeamIndex.Player);
        }

        public CombatSquad SpawnMonsters(
            string spawnCard,
            EliteIndex eliteIndex = EliteIndex.None,
            CharacterBody targetBody = null,
            int numMonsters = 1,
            EventDirector director = null)
        {
            return SpawnMonstersInternal(spawnCard, eliteIndex, targetBody, numMonsters, director, TeamIndex.Monster);
        }

        private CombatSquad SpawnMonstersInternal(
            string spawnCard,
            EliteIndex eliteIndex,
            CharacterBody targetBody,
            int numMonsters,
            EventDirector director,
            TeamIndex teamIndex)
        {
            CombatSquad group = gameObject.AddComponent<CombatSquad>();

            IDisposable chargingHandle = null;
            if (director)
            {
                chargingHandle = director.CreateOpenTeleporterObjectiveHandle();
            }
            group.onMemberLost += (master) =>
            {
                if (group.memberCount == 0)
                {
                    chargingHandle?.Dispose();
                    Destroy(group);
                }
            };

            DirectorPlacementRule placementRule;
            if (targetBody)
            {
                placementRule = new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                    minDistance = 8f,
                    maxDistance = 20f,
                    spawnOnTarget = targetBody.transform
                };
            }
            else
            {
                placementRule = new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Random,
                };
            }

            SpawnCard card = Resources.Load<SpawnCard>(spawnCard);
            if (!card)
            {
                Destroy(group);
                return null;
            }
            card.directorCreditCost = 0;
            SpawnFinalizer finalizer = new SpawnFinalizer()
            {
                CombatSquad = group,
                EliteIndex = eliteIndex,
                IsBoss = teamIndex != TeamIndex.Player,
            };
            DirectorSpawnRequest request = new DirectorSpawnRequest(card, placementRule, RoR2Application.rng)
            {
                ignoreTeamMemberLimit = true,
                teamIndexOverride = teamIndex,
                onSpawnedServer = finalizer.OnCardSpawned,
            };
            if (teamIndex == TeamIndex.Player)
            {
                request.summonerBodyObject = targetBody.gameObject;
            }
            bool spawnedAny = false;
            for (int i = 0; i < numMonsters; ++i)
            {
                GameObject obj = DirectorCore.instance.TrySpawnObject(request);

                // Try one last time with Random placement
                if (!obj)
                {
                    Debug.LogWarning("Could not spawn monster near target body, spawning randomly on map");
                    DirectorSpawnRequest randomRequest = new DirectorSpawnRequest(
                        card,
                        new DirectorPlacementRule
                        {
                            placementMode = DirectorPlacementRule.PlacementMode.Random,
                        },
                        RoR2Application.rng)
                    {
                        ignoreTeamMemberLimit = true,
                        teamIndexOverride = teamIndex,
                        onSpawnedServer = finalizer.OnCardSpawned,
                    };
                    if (teamIndex == TeamIndex.Player)
                    {
                        randomRequest.summonerBodyObject = targetBody.gameObject;
                    }
                    obj = DirectorCore.instance.TrySpawnObject(randomRequest);
                }

                if (obj) {
                    spawnedAny = true;
                }
            }

            if (!spawnedAny)
            {
                Debug.LogError("Couldn't spawn any monster!");
                chargingHandle?.Dispose();
                Destroy(group);
                return null;
            }

            return group;
        }

        private class SpawnFinalizer
        {
            public CombatSquad CombatSquad { get; set; }

            public EliteIndex EliteIndex { get; set; }

            public bool IsBoss { get; set; }

            public void OnCardSpawned(SpawnCard.SpawnResult spawnResult)
            {
                if (!spawnResult.success)
                {
                    return;
                }

                CharacterMaster monster = spawnResult.spawnedInstance.GetComponent<CharacterMaster>();
                if (CombatSquad)
                {
                    CombatSquad.AddMember(monster);
                }

                EliteDef eliteDef = EliteCatalog.GetEliteDef(EliteIndex);
                EquipmentIndex equipmentIndex = (eliteDef != null) ? eliteDef.eliteEquipmentDef.equipmentIndex : EquipmentIndex.None;
                if (equipmentIndex != EquipmentIndex.None)
                {
                    monster.inventory.SetEquipmentIndex(equipmentIndex);
                }
                float healthBoostCoefficient = 1f;
                float damageBoostCoefficient = 1f;
                healthBoostCoefficient += Run.instance.difficultyCoefficient / 1.5f;
                damageBoostCoefficient += Run.instance.difficultyCoefficient / 15f;
                int numberOfPlayers = Mathf.Max(1, Run.instance.livingPlayerCount);
                healthBoostCoefficient *= Mathf.Pow(numberOfPlayers, 0.75f);
                monster.inventory.GiveItem(RoR2Content.Items.BoostHp, Mathf.RoundToInt(Mathf.RoundToInt(healthBoostCoefficient - 1f) * 10f));
                monster.inventory.GiveItem(RoR2Content.Items.BoostDamage, Mathf.RoundToInt(Mathf.RoundToInt(damageBoostCoefficient - 1f) * 10f));

                monster.isBoss = IsBoss;
            }
        }
        

        public class Monsters
        {
            private Monsters() { }
            public static readonly string ArchWisp = "SpawnCards/CharacterSpawnCards/cscArchWisp";
            public static readonly string BackupDrone = "SpawnCards/CharacterSpawnCards/cscBackupDrone";
            public static readonly string Beetle = "SpawnCards/CharacterSpawnCards/cscBeetle";
            public static readonly string BeetleCrystal = "SpawnCards/CharacterSpawnCards/cscBeetleCrystal";
            public static readonly string BeetleGuard = "SpawnCards/CharacterSpawnCards/cscBeetleGuard";
            public static readonly string BeetleGuardAlly = "SpawnCards/CharacterSpawnCards/cscBeetleGuardAlly";
            public static readonly string BeetleGuardCrystal = "SpawnCards/CharacterSpawnCards/cscBeetleGuardCrystal";
            public static readonly string BeetleQueen = "SpawnCards/CharacterSpawnCards/cscBeetleQueen";
            public static readonly string Bell = "SpawnCards/CharacterSpawnCards/cscBell";
            public static readonly string Bison = "SpawnCards/CharacterSpawnCards/cscBison";
            public static readonly string Brother = "SpawnCards/CharacterSpawnCards/cscBrother";
            public static readonly string BrotherGlass = "SpawnCards/CharacterSpawnCards/cscBrotherGlass";
            public static readonly string BrotherHurt = "SpawnCards/CharacterSpawnCards/cscBrotherHurt";
            public static readonly string ClayBoss = "SpawnCards/CharacterSpawnCards/cscClayBoss";
            public static readonly string ClayBruiser = "SpawnCards/CharacterSpawnCards/cscClayBruiser";
            public static readonly string ElectricWorm = "SpawnCards/CharacterSpawnCards/cscElectricWorm";
            public static readonly string Golem = "SpawnCards/CharacterSpawnCards/cscGolem";
            public static readonly string Gravekeeper = "SpawnCards/CharacterSpawnCards/cscGravekeeper";
            public static readonly string GreaterWisp = "SpawnCards/CharacterSpawnCards/cscGreaterWisp";
            public static readonly string HermitCrab = "SpawnCards/CharacterSpawnCards/cscHermitCrab";
            public static readonly string Imp = "SpawnCards/CharacterSpawnCards/cscImp";
            public static readonly string ImpBoss = "SpawnCards/CharacterSpawnCards/cscImpBoss";
            public static readonly string Jellyfish = "SpawnCards/CharacterSpawnCards/cscJellyfish";
            public static readonly string Lemurian = "SpawnCards/CharacterSpawnCards/cscLemurian";
            public static readonly string LemurianBruiser = "SpawnCards/CharacterSpawnCards/cscLemurianBruiser";
            public static readonly string LesserWisp = "SpawnCards/CharacterSpawnCards/cscLesserWisp";
            public static readonly string LunarGolem = "SpawnCards/CharacterSpawnCards/cscLunarGolem";
            public static readonly string LunarWisp = "SpawnCards/CharacterSpawnCards/cscLunarWisp";
            public static readonly string MagmaWorm = "SpawnCards/CharacterSpawnCards/cscMagmaWorm";
            public static readonly string MiniMushroom = "SpawnCards/CharacterSpawnCards/cscMiniMushroom";
            public static readonly string Nullifier = "SpawnCards/CharacterSpawnCards/cscNullifier";
            public static readonly string Parent = "SpawnCards/CharacterSpawnCards/cscParent";
            public static readonly string ParentPod = "SpawnCards/CharacterSpawnCards/cscParentPod";
            public static readonly string RoboBallBoss = "SpawnCards/CharacterSpawnCards/cscRoboBallBoss";
            public static readonly string RoboBallMini = "SpawnCards/CharacterSpawnCards/cscRoboBallMini";
            public static readonly string Scav = "SpawnCards/CharacterSpawnCards/cscScav";
            public static readonly string ScavLunar = "SpawnCards/CharacterSpawnCards/cscScavLunar";
            public static readonly string SquidTurret = "SpawnCards/CharacterSpawnCards/cscSquidTurret";
            public static readonly string SuperRoboBallBoss = "SpawnCards/CharacterSpawnCards/cscSuperRoboBallBoss";
            public static readonly string TitanGold = "SpawnCards/CharacterSpawnCards/cscTitanGold";
            public static readonly string TitanGoldAlly = "SpawnCards/CharacterSpawnCards/cscTitanGoldAlly";
            public static readonly string Vagrant = "SpawnCards/CharacterSpawnCards/cscVagrant";
            public static readonly string Vulture = "SpawnCards/CharacterSpawnCards/cscVulture";
            public static readonly string Grandparent = "SpawnCards/CharacterSpawnCards/cscGrandparent";
            public static readonly string TitanBlackBeach = "SpawnCards/CharacterSpawnCards/cscTitanBlackBeach";
            public static readonly string TitanDampCave = "SpawnCards/CharacterSpawnCards/cscTitanDampCave";
            public static readonly string TitanGolemPlains = "SpawnCards/CharacterSpawnCards/cscTitanGolemPlains";
            public static readonly string TitanGooLake = "SpawnCards/CharacterSpawnCards/cscTitanGooLake";
        }
    }
}
