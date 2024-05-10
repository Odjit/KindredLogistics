using Il2CppInterop.Runtime;
using KindredLogistics.Patches;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace KindredLogistics
{
    public static class Utilities
    {
        public static readonly ComponentType[] StashQuery =
            [
                ComponentType.ReadOnly(Il2CppType.Of<InventoryOwner>()),
                ComponentType.ReadOnly(Il2CppType.Of<CastleHeartConnection>()),
                ComponentType.ReadOnly(Il2CppType.Of<AttachedBuffer>()),
                ComponentType.ReadOnly(Il2CppType.Of<NameableInteractable>()),
            ];

        public static readonly ComponentType[] RefinementStationQuery =
            [
                ComponentType.ReadOnly(Il2CppType.Of<Team>()),
                ComponentType.ReadOnly(Il2CppType.Of<CastleHeartConnection>()),
                ComponentType.ReadOnly(Il2CppType.Of<Refinementstation>()),
                ComponentType.ReadOnly(Il2CppType.Of<NameableInteractable>()),
            ];

        public static readonly ComponentType[] UserEntityQuery =
        [
                ComponentType.ReadOnly(Il2CppType.Of<User>()),
        ];

        public static void StashServantInventory(Entity servant)
        {
            var serverGameManager = Core.ServerGameManager;
            // need character entity of this platformId, query for User and match id then get character entity?
            var stashes = UpdateRefiningSystemPatch.stashesQuery.ToEntityArray(Allocator.TempJob);
            var matches = new Dictionary<PrefabGUID, List<(Entity station, int amount)>>(capacity: 100);
            // Find matches to use for autostash
            // want to do query for appropriate stash with missions in the name as well for any loot that cant be autostashed
            var missionStash = Entity.Null;
            try
            {
                foreach (var stash in stashes)
                {
                    if (!Core.EntityManager.Exists(stash)) continue;
                    if (Utilities.SharedHeartConnection(servant, stash) && stash.Read<NameableInteractable>().Name.ToString().ToLower().Contains("spoils") && missionStash.Equals(Entity.Null)) // store mission stash for later
                    {
                        missionStash = stash;
                        break;
                    }
                    if (serverGameManager.TryGetBuffer<AttachedBuffer>(stash, out var buffer))
                    {
                        Entity externalInventory = Entity.Null;
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
                        if (serverGameManager.TryGetBuffer<InventoryBuffer>(externalInventory, out var inventoryBuffer) && !inventoryBuffer.IsEmpty)
                        {
                            for (int i = 0; i < inventoryBuffer.Length; i++)
                            {
                                PrefabGUID item = inventoryBuffer[i].ItemType;
                                if (item.GuidHash == 0) continue;
                                if (!matches.ContainsKey(item)) matches[item] = [];
                                else if (matches.TryGetValue(item, out var list) && list.Any(entry => entry.station == stash)) continue;
                                matches[item].Add((stash, serverGameManager.GetInventoryItemCount(externalInventory, item)));
                            }
                        }
                        else
                        {
                            //Core.Log.LogInfo("No inventoryBuffer found for external inventory.");
                        }
                    }
                    else
                    {
                        //Core.Log.LogInfo("No AttachedBuffer found for entity.");
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
            if (InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, servant, out Entity inventory))
            {
                if (serverGameManager.TryGetBuffer<InventoryBuffer>(inventory, out var buffer))
                {
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        var item = buffer[i].ItemType;
                        if (item.GuidHash == 0) continue;
                        if (matches.TryGetValue(item, out List<(Entity, int)> stashEntities))
                        {
                            foreach (var stash in stashEntities)
                            {
                                if (!Utilities.SharedHeartConnection(servant, stash.Item1))
                                {
                                    Core.Log.LogInfo("Servant doesn't have the same heart connection, skipping stash...");
                                    continue;
                                }

                                if (!InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, stash.Item1, out Entity stashInventory))
                                {
                                    //Core.Log.LogInfo("No stash inventory entity found for stash during auto-stashing.");
                                    continue;
                                }

                                int transferAmount = serverGameManager.GetInventoryItemCount(inventory, item);
                                Utilities.TransferItems(serverGameManager, inventory, stashInventory, item, transferAmount);
                            }
                        }
                        else
                        {
                            // if no match found for autostash, send to 'missions' stash
                            if (!missionStash.Equals(Entity.Null))
                            {
                                if (!InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, missionStash, out Entity missionInventory))
                                {
                                    Core.Log.LogInfo("No inventory entity found for missions stash...");
                                    continue;
                                }

                                int transferAmount = serverGameManager.GetInventoryItemCount(inventory, item);
                                Utilities.TransferItems(serverGameManager, inventory, missionInventory, item, transferAmount);
                            }
                            else
                            {
                                Core.Log.LogInfo("No matches and no missions stash found, skipping...");
                            }
                        }
                    }
                }
                else
                {
                    Core.Log.LogInfo($"No inventory buffer found for servant...");
                }
            }
            else
            {
                Core.Log.LogInfo($"No inventory entity found for servant...");
            }
        }

        public static bool TerritoryCheck(Entity character, Entity target)
        {
            if (!target.Has<CastleHeartConnection>())
                return false;

            var charPos = character.Read<TilePosition>();
            var heart = target.Read<CastleHeartConnection>().CastleHeartEntity.GetEntityOnServer();
            var castleHeart = heart.Read<CastleHeart>();
            var castleTerritory = castleHeart.CastleTerritoryEntity;
            return CastleTerritoryExtensions.IsTileInTerritory(Core.EntityManager, charPos.Tile, ref castleTerritory, out var _);
        }

        public static bool SharedHeartConnection(Entity input, Entity ouput)
        {
            if (input.Has<CastleHeartConnection>() && ouput.Has<CastleHeartConnection>())
            {
                var inputHeart = input.Read<CastleHeartConnection>().CastleHeartEntity._Entity;
                var outputHeart = ouput.Read<CastleHeartConnection>().CastleHeartEntity._Entity;
                return inputHeart.Equals(outputHeart);
            }
            return false;
        }

        public static void TransferItems(ServerGameManager serverGameManager, Entity outputInventory, Entity inputInventory, PrefabGUID itemGuid, int transferAmount)
        {
            if (serverGameManager.TryRemoveInventoryItem(outputInventory, itemGuid, transferAmount))
            {
                if (serverGameManager.TryAddInventoryItem(inputInventory, itemGuid, transferAmount))
                {
                    Core.Log.LogInfo($"Moved {transferAmount} of {itemGuid.LookupName()} from Input to Output");
                }
                else
                {
                    Core.Log.LogInfo($"Failed to add {itemGuid.LookupName()}x{transferAmount} to OutputInventory, reverting...");
                    if (serverGameManager.TryAddInventoryItem(outputInventory, itemGuid, transferAmount))
                    {
                        Core.Log.LogInfo($"Restored items to original inventory.");
                    }
                    else
                    {
                        Core.Log.LogInfo($"Unable to return items to original inventory.");
                    }
                }
            }
            else
            {
                Core.Log.LogInfo($"Failed to remove {itemGuid.LookupName()}x{transferAmount} from Input");
            }
        }
    }
}
