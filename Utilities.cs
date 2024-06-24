using Il2CppInterop.Runtime;
using KindredLogistics.Services;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Entities;

namespace KindredLogistics
{
    public class Utilities
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
            var matches = new Dictionary<PrefabGUID, List<(Entity stash, Entity inventory)>>(capacity: 100);
            (Entity stash, Entity inventory) missionStash = (Entity.Null, Entity.Null);
            try
            {
                foreach (Entity stash in Core.Stash.GetAllAlliedStashesOnTerritory(servant))
                {
                    if (stash.Read<NameableInteractable>().Name.ToString().ToLower().Contains("spoils") && missionStash.stash.Equals(Entity.Null)) // store mission stash for later
                    {
                        if (!InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, stash, out Entity missionInventory)) continue;
                        if (!serverGameManager.HasFullInventory(missionInventory))
                        {
                            missionStash = (stash, missionInventory);
                            continue;
                        }
                    }
                    if (!serverGameManager.TryGetBuffer<AttachedBuffer>(stash, out var buffer))
                        continue;

                    foreach (var attachedBuffer in buffer)
                    {
                        Entity attachedEntity = attachedBuffer.Entity;
                        if (!attachedEntity.Has<PrefabGUID>()) continue;
                        if (!attachedEntity.Read<PrefabGUID>().Equals(StashService.ExternalInventoryPrefab)) continue;

                        var checkInventoryBuffer = attachedEntity.ReadBuffer<InventoryBuffer>();
                        foreach (var inventoryEntry in checkInventoryBuffer)
                        {
                            var item = inventoryEntry.ItemType;
                            if (item.GuidHash == 0) continue;
                            if (!matches.TryGetValue(item, out var itemMatches))
                            {
                                itemMatches = [];
                                matches[item] = itemMatches;
                            }
                            else if (itemMatches.Any(x => x.stash == stash)) continue;
                            itemMatches.Add((stash, attachedEntity));
                        }
                    }
                }
                if (!InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, servant, out Entity inventory))
                    return;

                if (!serverGameManager.TryGetBuffer<InventoryBuffer>(inventory, out var inventoryBuffer))
                    return;
                for (int i = 0; i < inventoryBuffer.Length; i++)
                {
                    var item = inventoryBuffer[i].ItemType;
                    if (!matches.TryGetValue(item, out var stashEntries)) // if no match straight to spoils
                    {
                        if (missionStash.stash.Equals(Entity.Null)) continue;
                        int transferAmount = serverGameManager.GetInventoryItemCount(inventory, item);
                        TransferItems(serverGameManager, inventory, missionStash.inventory, item, transferAmount);
                        continue;
                    }

                    foreach (var stashEntry in stashEntries) // if match stash first, then spoils if no room
                    {
                        int transferAmount = serverGameManager.GetInventoryItemCount(inventory, item);
                        int amountTransferred = TransferItems(serverGameManager, inventory, stashEntry.inventory, item, transferAmount); // returns amount transferred
                        int remaining = transferAmount - amountTransferred;
                        if (remaining > 0 && !missionStash.stash.Equals(Entity.Null)) // send remaining to spoils
                        {
                            //Core.Log.LogInfo($"Transferred {amountTransferred} to matching stash with {remaining} left for spoils...");
                            int remainingAmountTransferred = TransferItems(serverGameManager, inventory, missionStash.inventory, item, remaining);
                            //Core.Log.LogInfo($"Transferred {remainingAmountTransferred} to spoils. Remaining in inventory: {serverGameManager.GetInventoryItemCount(inventory, item)}");
                        }
                    }
                    
                }
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Exited StashServantInventory early: {e}");
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

        public static bool TransferItemEntites(Entity outputInventory, Entity inputInventory, PrefabGUID itemPrefab, int transferAmount, ref int startInputSlot, out int amountTransferred)
        {
            var outputBuffer = outputInventory.ReadBuffer<InventoryBuffer>();
            var inputBuffer = inputInventory.ReadBuffer<InventoryBuffer>();

            amountTransferred = 0;

            for (int i = 0; i < outputBuffer.Length; i++)
            {
                var outputItem = outputBuffer[i];
                if (!outputItem.ItemType.Equals(itemPrefab)) continue;

                while (startInputSlot < inputBuffer.Length)
                {
                    var inputSlot = inputBuffer[startInputSlot];
                    if (!inputSlot.ItemType.Equals(PrefabGUID.Empty))
                    {
                        startInputSlot++;
                        continue;
                    }
                    inputBuffer[startInputSlot] = outputItem;
                    outputBuffer[i] = inputSlot;

                    var itemEntity = outputItem.ItemEntity.GetEntityOnServer();
                    if (itemEntity.Has<InventoryItem>())
                    {
                        var inventoryItem = itemEntity.Read<InventoryItem>();
                        inventoryItem.ContainerEntity = inputInventory;
                        itemEntity.Write(inventoryItem);
                    }

                    startInputSlot++;
                    amountTransferred++;
                    break;
                }

                if (amountTransferred >= transferAmount)
                {
                    return false;
                }

                if (inputBuffer.Length <= startInputSlot)
                {
                    return true;
                }
            }
            return false;
        }

        public static int TransferItems(ServerGameManager serverGameManager, Entity outputInventory, Entity inputInventory, PrefabGUID itemGuid, int transferAmount)
        {
            if (serverGameManager.TryRemoveInventoryItem(outputInventory, itemGuid, transferAmount))
            {
                var response = serverGameManager.TryAddInventoryItem(inputInventory, itemGuid, transferAmount);
                
                if (response.Result == AddItemResult.Success_Complete)
                {
                    //Core.Log.LogInfo($"Moved {transferAmount} of {itemGuid.LookupName()} from Input to Output");
                    return transferAmount;
                }
                else
                {
                    //Core.Log.LogInfo($"Failed to add {itemGuid.LookupName()}x{transferAmount} to OutputInventory, restoring {response.RemainingAmount}...");
                    var restoreResponse = serverGameManager.TryAddInventoryItem(outputInventory, itemGuid, response.RemainingAmount);
                    if (restoreResponse.Result == AddItemResult.Success_Complete)
                    {
                        //Core.Log.LogInfo($"Restored items to original inventory.");
                    }
                    else
                    {
                        //Core.Log.LogInfo($"Unable to return items to original inventory.");
                    }
                    return transferAmount - response.RemainingAmount;
                }
            }
            else
            {
                //Core.Log.LogInfo($"Failed to remove {itemGuid.LookupName()}x{transferAmount} from Input");
            }
            return 0;
        }

        public static AddItemSettings GetAddItemSettings()
        {
            AddItemSettings addItemSettings = default;
            addItemSettings.EntityManager = Core.EntityManager;
            unsafe
            {
                // Pin the buffer object to prevent the GC from moving it while we access it via pointers
                GCHandle handle = GCHandle.Alloc(Core.ServerGameManager.ItemLookupMap, GCHandleType.Pinned);
                try
                {
                    // Obtain the actual address of the buffer
                    IntPtr address = handle.AddrOfPinnedObject();

                    // Assuming the buckets pointer is the first field in the buffer struct
                    // You may need to adjust the offset depending on the actual memory layout
                    addItemSettings.ItemDataMap = Marshal.ReadIntPtr(address);
                }
                finally
                {
                    if (handle.IsAllocated)
                        handle.Free();
                }
            }
            return addItemSettings;
        }
    }
}