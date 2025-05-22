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
using Unity.Collections;
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
            if (!InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, servant, out Entity inventory))
                return;

            StashInventoryEntity(servant, inventory, "spoils");
        }

        public static void StashInventoryEntity(Entity entityWithTerritory, Entity inventory, string overflowStashName)
        {
            var serverGameManager = Core.ServerGameManager;
            var matches = new Dictionary<PrefabGUID, List<(Entity stash, Entity inventory)>>(capacity: 100);
            (Entity stash, Entity inventory) overflowStash = (Entity.Null, Entity.Null);
            try
            {
                foreach (Entity stash in Core.Stash.GetAllAlliedStashesOnTerritory(entityWithTerritory))
                {
                    if (stash.Read<NameableInteractable>().Name.ToString().ToLower().Contains(overflowStashName) && overflowStash.stash.Equals(Entity.Null)) // store mission stash for later
                    {
                        if (!InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, stash, out Entity missionInventory)) continue;
                        if (!serverGameManager.HasFullInventory(missionInventory))
                        {
                            overflowStash = (stash, missionInventory);
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

                if (!serverGameManager.TryGetBuffer<InventoryBuffer>(inventory, out var inventoryBuffer))
                    return;
                for (var i = 0; i < inventoryBuffer.Length; i++)
                {
                    var item = inventoryBuffer[i].ItemType;
                    var amountToTransfer = serverGameManager.GetInventoryItemCount(inventory, item);
                    if (matches.TryGetValue(item, out var stashEntries)) // if no match straight to spoils
                    {
                        foreach (var stashEntry in stashEntries) // if match stash first, then spoils if no room
                        {
                            amountToTransfer -= TransferItems(serverGameManager, inventory, stashEntry.inventory, item, amountToTransfer); // returns amount transferred
                            if (amountToTransfer <= 0) break;
                        }
                    }

                    if (amountToTransfer > 0 && !overflowStash.stash.Equals(Entity.Null)) // send remaining to spoils
                    {
                        //Core.Log.LogInfo($"Transferred {amountTransferred} to matching stash with {remaining} left for spoils...");
                        var remainingAmountTransferred = TransferItems(serverGameManager, inventory, overflowStash.inventory, item, amountToTransfer);
                        //Core.Log.LogInfo($"Transferred {remainingAmountTransferred} to spoils. Remaining in inventory: {serverGameManager.GetInventoryItemCount(inventory, item)}");
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

        static void CheckIfInventoryEmpty(Entity inventory)
        {
            var invBuffer = inventory.ReadBuffer<InventoryBuffer>();
            for (int i=0; i<invBuffer.Length; i++)
            {
                if (invBuffer[i].Amount != 0) return;
            }

            var inventoryOwner = inventory.Read<InventoryOwner>();
            inventoryOwner.HasItems = false;
            inventory.Write(inventoryOwner);

            var connectionEntity = inventory.Read<InventoryConnection>().InventoryOwner;
            var invOwnerOnConnection = connectionEntity.Read<InventoryOwner>();
            invOwnerOnConnection.HasItems = false;
            connectionEntity.Write(invOwnerOnConnection);
        }

        public static bool TransferItemEntities(Entity outputInventory, Entity inputInventory, PrefabGUID itemPrefab, int transferAmount, ref int startInputSlot, out int amountTransferred)
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
                    CheckIfInventoryEmpty(outputInventory);
                    return false;
                }

                if (inputBuffer.Length <= startInputSlot)
                {
                    CheckIfInventoryEmpty(outputInventory);
                    return true;
                }
            }
            CheckIfInventoryEmpty(outputInventory);
            return false;
        }

        public static int TransferItems(ServerGameManager serverGameManager, Entity outputInventory, Entity inputInventory, PrefabGUID itemGuid, int transferAmount)
        {
            if (serverGameManager.TryRemoveInventoryItem(outputInventory, itemGuid, transferAmount))
            {
                var response = serverGameManager.TryAddInventoryItem(inputInventory, itemGuid, transferAmount);
                
                if (response.Result == AddItemResult.Success_Complete)
                {
                    //Core.Log.LogInfo($"Moved {amountToTransfer} of {itemGuid.LookupName()} from Input to Output");
                    return transferAmount;
                }
                else
                {
                    //Core.Log.LogInfo($"Failed to add {itemGuid.LookupName()}x{amountToTransfer} to OutputInventory, restoring {response.RemainingAmount}...");
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
                //Core.Log.LogInfo($"Failed to remove {itemGuid.LookupName()}x{amountToTransfer} from Input");
            }
            return 0;
        }

        public static AddItemSettings GetAddItemSettings()
        {
            AddItemSettings addItemSettings = default;
            addItemSettings.EntityManager = Core.EntityManager;
            addItemSettings.ItemDataMap = Core.ServerGameManager.ItemLookupMap;
            return addItemSettings;
        }

        public static void SendSystemMessageToClient(EntityManager entityManager, User user, string message)
        {
            var msg = new FixedString512Bytes(message);
            ServerChatUtils.SendSystemMessageToClient(entityManager, user, ref msg);
        }
    }
}