using RoR2;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace VsTwitch
{
    /// <summary>
    /// Utility class for giving items to players.
    /// </summary>
    class ItemManager
    {
        public static void GiveToAllPlayers(PickupIndex pickupIndex)
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'System.Void ItemManager::GiveToAllPlayers()' called on client");
                return;
            }

            foreach (var controller in PlayerCharacterMasterController.instances)
            {
                var player = controller.master;
                GiveToPlayer(player, pickupIndex);
            }
        }

        public static void GiveToPlayer(CharacterMaster characterMaster, PickupIndex pickupIndex)
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'System.Void ItemManager::GiveToPlayer()' called on client");
                return;
            }

            characterMaster.inventory.GiveItem(PickupCatalog.GetPickupDef(pickupIndex).itemIndex, 1);

            // Broadcast message that pickup happened
            MethodInfo sendPickupMessage = typeof(GenericPickupController).GetMethod("SendPickupMessage", BindingFlags.Static | BindingFlags.NonPublic);
            sendPickupMessage.Invoke(null, new object[] { characterMaster, pickupIndex });
        }

        public static void DropToAllPlayers(PickupIndex pickupIndex, Vector3 velocity)
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'System.Void ItemManager::DropToAllPlayers()' called on client");
                return;
            }

            foreach (var controller in PlayerCharacterMasterController.instances)
            {
                var player = controller.master;
                DropToPlayer(player, pickupIndex, velocity);
            }
        }

        public static void DropToPlayer(CharacterMaster characterMaster, PickupIndex pickupIndex, Vector3 velocity)
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'System.Void ItemManager::DropToPlayer()' called on client");
                return;
            }

            var body = characterMaster.GetBody();
            if (body != null)
            {
                PickupDropletController.CreatePickupDroplet(pickupIndex, body.corePosition + Vector3.up * 1.5f, velocity);
            }
        }
    }
}
