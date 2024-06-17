using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Network;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;

namespace KindredLogistics.Services
{
    internal class StashService
    {
        const int ACTION_BAR_SLOTS = 8;
        const string SKIP_SUFFIX = "''";
        const float FIND_SPOTLIGHT_DURATION = 15f;

        static readonly ComponentType[] StashQuery =
            [
                ComponentType.ReadOnly(Il2CppType.Of<InventoryOwner>()),
                ComponentType.ReadOnly(Il2CppType.Of<CastleHeartConnection>()),
                ComponentType.ReadOnly(Il2CppType.Of<AttachedBuffer>()),
                ComponentType.ReadOnly(Il2CppType.Of<NameableInteractable>()),
            ];

        public static readonly PrefabGUID ExternalInventoryPrefab = new(1183666186);
        static readonly PrefabGUID findContainerSpotlightPrefab = new(-2014639169);

        public delegate bool StashFilter(Entity station);

        EntityQuery stashQuery;
        readonly Regex receiverRegex;
        readonly Regex senderRegex;

        Dictionary<Entity, (double expirationTime, List<Entity> targetStashes)> activeSpotlights = [];

        public StashService()
        {
            stashQuery = Core.EntityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = StashQuery,
                Options = EntityQueryOptions.IncludeDisabledEntities
            });
            receiverRegex = new Regex(Const.RECEIVER_REGEX, RegexOptions.Compiled);
            senderRegex = new Regex(Const.SENDER_REGEX, RegexOptions.Compiled);
        }

        public IEnumerable<Entity> GetAllAlliedStashesOnTerritory(Entity character)
        {
            var territoryIndex = Core.TerritoryService.GetTerritoryId(character);
            var serverGameManager = Core.ServerGameManager;
            NativeArray<Entity> stashArray = stashQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var stash in stashArray)
                {
                    try
                    {
                        if (Core.TerritoryService.GetTerritoryId(stash) != territoryIndex) continue;
                        if (!serverGameManager.IsAllies(stash, character)) continue;

                        var name = stash.Read<NameableInteractable>().Name.ToString();
                        if (name.EndsWith(SKIP_SUFFIX)) continue;
                    }
                    catch (Exception e)
                    {
                        Core.LogException(e, "Yielding Stashes");
                        continue;
                    }

                    yield return stash;
                }
            }
            finally
            {
                stashArray.Dispose();
            }
        }

        public IEnumerable<(int group, Entity station)> GetAllReceivingStashes(int territoryId)
        {
            foreach (var result in GetAllGroupStations(receiverRegex, territoryId))
            {
                yield return result;
            }
        }

        public IEnumerable<(int group, Entity station)> GetAllSendingStashes(int territoryId)
        {
            foreach (var result in GetAllGroupStations(senderRegex, territoryId))
            {
                yield return result;
            }
        }

        IEnumerable<(int group, Entity station)> GetAllGroupStations(Regex groupRegex, int territoryId)
        {
            var stashArray = stashQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var stash in stashArray)
                {
                    var stashTerritoryId = Core.TerritoryService.GetTerritoryId(stash);
                    if (stashTerritoryId != territoryId)
                        continue;

                    var name = stash.Read<NameableInteractable>().Name.ToString().ToLower();
                    foreach (Match match in groupRegex.Matches(name))
                    {
                        var group = int.Parse(match.Groups[1].Value);
                        yield return (group, stash);
                    }
                }
            }
            finally
            {
                stashArray.Dispose();
            }
        }

        public void StashCharacterInventory(Entity charEntity)
        {
            try
            {
                var userEntity = charEntity.Read<PlayerCharacter>().UserEntity;
                var user = userEntity.Read<User>();

                var territoryIndex = Core.TerritoryService.GetTerritoryId(charEntity);
                if (territoryIndex == -1)
                {
                    ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "Unable to stash outside territories!");
                    return;
                }

                var serverGameManager = Core.ServerGameManager;
                var matches = new Dictionary<PrefabGUID, List<(Entity stash, Entity inventory)>>(capacity: 100);
                var foundStash = false;
                foreach (var stash in GetAllAlliedStashesOnTerritory(charEntity))
                {
                    try
                    {
                        if (stash.Has<CastleWorkstation>()) continue;
                        if (!serverGameManager.TryGetBuffer<AttachedBuffer>(stash, out var buffer))
                            continue;

                        foundStash = true;

                        foreach (var attachedBuffer in buffer)
                        {
                            var attachedEntity = attachedBuffer.Entity;
                            if (!attachedEntity.Has<PrefabGUID>()) continue;
                            if (!attachedEntity.Read<PrefabGUID>().Equals(ExternalInventoryPrefab)) continue;

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
                    catch (Exception e)
                    {
                        Core.LogException(e, "Stash Retrieval");
                    }
                }

                if (!foundStash)
                {
                    ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "Unable to stash as no available stashes found in your current territory!");
                    return;
                }

                // get player inventory and find allied owned stashes in same territory with item matches
                if (!InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, charEntity, out Entity inventory))
                    return;

                if (!serverGameManager.TryGetBuffer<InventoryBuffer>(inventory, out var inventoryBuffer))
                    return;

                var addItemSettings = Utilities.GetAddItemSettings();
                HashSet<PrefabGUID> transferredItems = [];
                Dictionary<(Entity stash, PrefabGUID item), int> amountStashed = [];
                Dictionary<PrefabGUID, int> amountUnstashed = [];
                for (int i = ACTION_BAR_SLOTS; i < inventoryBuffer.Length; i++)
                {
                    var itemEntry = inventoryBuffer[i];
                    var item = itemEntry.ItemType;
                    if (!matches.TryGetValue(item, out var stashEntries)) continue;

                    var hasItemEntity = !itemEntry.ItemEntity.GetEntityOnServer().Equals(Entity.Null);

                    if (hasItemEntity)
                    {
                        var success = false;
                        foreach (var stashEntry in stashEntries)
                        {
                            try
                            {
                                var stashInventoryBuffer = stashEntry.inventory.ReadBuffer<InventoryBuffer>();

                                for (int j = 0; j < stashInventoryBuffer.Length; j++)
                                {
                                    if (!stashInventoryBuffer[j].ItemType.Equals(PrefabGUID.Empty)) continue;

                                    transferredItems.Add(item);
                                    stashInventoryBuffer[j] = itemEntry;

                                    var itemEntity = itemEntry.ItemEntity.GetEntityOnServer();
                                    if (itemEntity.Has<InventoryItem>())
                                    {
                                        var inventoryItem = itemEntity.Read<InventoryItem>();
                                        inventoryItem.ContainerEntity = stashEntry.stash;
                                        itemEntity.Write(inventoryItem);
                                    }

                                    if (amountStashed.TryGetValue((stashEntry.stash, item), out var amount))
                                        amountStashed[(stashEntry.stash, item)] = amount + 1;
                                    else
                                        amountStashed[(stashEntry.stash, item)] = 1;

                                    InventoryUtilitiesServer.ClearSlot(Core.EntityManager, inventory, i);
                                    success = true;
                                    break;
                                }

                                if (success) break;
                            }
                            catch (Exception e)
                            {
                                Core.LogException(e, "Item Entity Storage");
                            }
                        }
                        if (!success)
                        {
                            if (amountUnstashed.TryGetValue(item, out var amount))
                                amountUnstashed[item] = amount + 1;
                            else
                                amountUnstashed[item] = 1;
                        }
                    }
                    else
                    {
                        foreach (var stashEntry in stashEntries)
                        {
                            try
                            {
                                var addItemResponse = InventoryUtilitiesServer.TryAddItem(addItemSettings, stashEntry.inventory, itemEntry);

                                if (!addItemResponse.Success) continue;

                                transferredItems.Add(item);
                                var transferredAmount = itemEntry.Amount - addItemResponse.RemainingAmount;
                                if (amountStashed.TryGetValue((stashEntry.stash, item), out var amount))
                                    amountStashed[(stashEntry.stash, item)] = amount + transferredAmount;
                                else
                                    amountStashed[(stashEntry.stash, item)] = transferredAmount;

                                itemEntry.Amount = addItemResponse.RemainingAmount;
                                if (!addItemResponse.ItemsRemaining)
                                {
                                    InventoryUtilitiesServer.ClearSlot(Core.EntityManager, inventory, i);
                                    break;
                                }
                            }
                            catch (Exception e)
                            {
                                Core.LogException(e, "Item Storage");
                            }
                        }

                        if (itemEntry.Amount > 0)
                        {
                            inventoryBuffer[i] = itemEntry;

                            if (amountUnstashed.TryGetValue(item, out var amount))
                                amountUnstashed[item] = amount + itemEntry.Amount;
                            else
                                amountUnstashed[item] = itemEntry.Amount;
                        }
                    }
                }

                if (amountStashed.Count > 0)
                {
                    ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "Stashed items from your inventory to the current territory!");
                }
                else
                {
                    ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "No items were able to stash from your inventory!");
                }

                if (!Core.PlayerSettings.IsSilentStashEnabled(user.PlatformId))
                {
                    foreach (var ((stash, item), amount) in amountStashed)
                    {
                        ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user,
                                               $"Stashed <color=white>{amount}</color>x <color=green>{item.PrefabName()}</color> to <color=#FFC0CB>{stash.EntityName()}</color>");
                    }

                    foreach (var stashedItemType in transferredItems)
                    {
                        if (amountUnstashed.TryGetValue(stashedItemType, out var amount))
                            ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user,
                                                               $"Unable to stash <color=white>{amount}</color>x <color=green>{stashedItemType.PrefabName()}</color> due to insufficient space in stashes!");
                    }
                }
            }
            catch (Exception e)
            {
                Core.LogException(e, "Stash Character Inventory");
            }

        }

        public void ReportWhereItemIsLocated(Entity charEntity, PrefabGUID item)
        {
            var userEntity = charEntity.Read<PlayerCharacter>().UserEntity;
            var user = userEntity.Read<User>();

            ClearSpotlights(userEntity);

            var territoryIndex = Core.TerritoryService.GetTerritoryId(charEntity);
            if (territoryIndex == -1)
            {
                ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "Unable to search for items outside territories!");
                return;
            }

            ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "Find Item Report\n--------------------------------");
            var serverGameManager = Core.ServerGameManager;
            var foundStash = false;
            var totalFound = 0;
            var itemName = item.PrefabName();
            foreach (var stash in GetAllAlliedStashesOnTerritory(charEntity))
            {
                if (!serverGameManager.TryGetBuffer<AttachedBuffer>(stash, out var buffer))
                    continue;

                foundStash = true;

                foreach (var attachedBuffer in buffer)
                {
                    var attachedEntity = attachedBuffer.Entity;
                    if (!attachedEntity.Has<PrefabGUID>()) continue;
                    if (!attachedEntity.Read<PrefabGUID>().Equals(ExternalInventoryPrefab)) continue;

                    var amountFound = serverGameManager.GetInventoryItemCount(attachedEntity, item);
                    if (amountFound > 0)
                    {
                        totalFound += amountFound;
                        ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user,
                                                       $"<color=white>{amountFound}</color>x <color=green>{item.PrefabName()}</color> found in <color=#FFC0CB>{stash.EntityName()}</color>");
                        AddSpotlight(stash, userEntity);
                    }
                }
            }

            if (!foundStash)
            {
                ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "No available stashes found in your current territory!");
                return;
            }

            ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, $"Total <color=green>{itemName}</color> found: <color=white>{totalFound}</color>");
        }

        void ClearSpotlights(Entity userEntity)
        {
            if (!activeSpotlights.TryGetValue(userEntity, out var spotlight))
                return;
            activeSpotlights.Remove(userEntity);

            if (spotlight.expirationTime < Core.ServerTime)
                return;

            foreach (var stash in spotlight.targetStashes)
            {
                Buffs.RemoveBuff(stash, findContainerSpotlightPrefab);
            }
        }

        void AddSpotlight(Entity stash, Entity userEntity)
        {
            if (!activeSpotlights.TryGetValue(userEntity, out var spotlight))
            {
                spotlight.expirationTime = Core.ServerTime + FIND_SPOTLIGHT_DURATION;
                spotlight.targetStashes = [];
                activeSpotlights.Add(userEntity, spotlight);
            }
            spotlight.targetStashes.Add(stash);

            Buffs.RemoveAndAddBuff(userEntity, stash, findContainerSpotlightPrefab, FIND_SPOTLIGHT_DURATION, UpdateSpotlight);

            void UpdateSpotlight(Entity buffEntity)
            {
                var character = userEntity.Read<User>().LocalCharacter;
                buffEntity.Write<SpellTarget>(new()
                {
                    Target = character
                });
                buffEntity.Write<EntityOwner>(new()
                {
                    Owner = character.GetEntityOnServer()
                });
                buffEntity.Write<EntityCreator>(new()
                {
                    Creator = character.GetEntityOnServer()
                });
            }
        }
    }
}