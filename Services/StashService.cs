using KindredLogistics.Patches;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace KindredLogistics.Services
{
    internal class StashService
    {
        public void StashCharacterInventory(Entity charEntity)
        {
            var userEntity = charEntity.Read<PlayerCharacter>().UserEntity;
            var user = userEntity.Read<User>();
            ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "Stashing your inventory to storage on your current territory");

            var serverGameManager = Core.ServerGameManager;
            var stashes = UpdateRefiningSystemPatch.stashesQuery.ToEntityArray(Allocator.TempJob);
            var matches = new Dictionary<PrefabGUID, List<(Entity station, int amount)>>(capacity: 100);
            try
            {
                foreach (var stash in stashes)
                {
                    if (!Core.EntityManager.Exists(stash)) continue;
                    if (serverGameManager.TryGetBuffer<AttachedBuffer>(stash, out var buffer))
                    {
                        var externalInventory = Entity.Null;
                        foreach (var external in buffer)
                        {
                            if (!external.Entity.Has<PrefabGUID>()) continue;
                            else if (external.Entity.Read<PrefabGUID>().Equals(UpdateRefiningSystemPatch.externalInventoryPrefab))
                            {
                                externalInventory = external.Entity;
                                break;
                            }
                        }
                        if (externalInventory.Equals(Entity.Null)) continue;
                        if (serverGameManager.TryGetBuffer<InventoryBuffer>(externalInventory, out var checkInventoryBuffer) && !checkInventoryBuffer.IsEmpty)
                        {
                            for (int i = 0; i < checkInventoryBuffer.Length; i++)
                            {
                                var item = checkInventoryBuffer[i].ItemType;
                                if (item.GuidHash == 0) continue;
                                if (!matches.ContainsKey(item)) matches[item] = [];
                                else if (matches.TryGetValue(item, out var list) && list.Any(entry => entry.station == stash)) continue;
                                matches[item].Add((stash, serverGameManager.GetInventoryItemCount(externalInventory, item)));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Exited UpdateRefiningSystem matchesProcessing early: {e}");
            }
            finally
            {
                stashes.Dispose();
            }

            // get player inventory and find allied owned stashes in same territory with item matches
            if (!InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, charEntity, out Entity inventory))
                return;

            if (!serverGameManager.TryGetBuffer<InventoryBuffer>(inventory, out var inventoryBuffer))
                return;

            for (int i = 8; i < inventoryBuffer.Length; i++)
            {
                var item = inventoryBuffer[i].ItemType;
                if (!matches.TryGetValue(item, out var stashEntities)) continue;
                        
                foreach (var stash in stashEntities)
                {
                    var stashOwner = stash.Item1.Read<UserOwner>().Owner.GetEntityOnServer();
                    if (!stashOwner.Equals(userEntity) || !Utilities.TerritoryCheck(charEntity, stash.Item1))
                        continue;

                    if (!InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, stash.Item1, out Entity stashInventory))
                        continue;

                    int transferAmount = serverGameManager.GetInventoryItemCount(inventory, item);
                    Utilities.TransferItems(serverGameManager, inventory, stashInventory, item, transferAmount);
                }
            }
        }
    }
}
