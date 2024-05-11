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

        static readonly ComponentType[] StashQuery =
            [
                ComponentType.ReadOnly(Il2CppType.Of<InventoryOwner>()),
                ComponentType.ReadOnly(Il2CppType.Of<CastleHeartConnection>()),
                ComponentType.ReadOnly(Il2CppType.Of<AttachedBuffer>()),
                ComponentType.ReadOnly(Il2CppType.Of<NameableInteractable>()),
            ];
        public static readonly PrefabGUID ExternalInventoryPrefab = new(1183666186);

        public delegate bool StashFilter(Entity station);

        EntityQuery stashQuery;
        readonly Regex receiverRegex;
        readonly Regex senderRegex;

        public StashService()
        {
            stashQuery = Core.EntityManager.CreateEntityQuery(StashQuery);
            receiverRegex = new Regex(Const.RECEIVER_REGEX, RegexOptions.Compiled);
            senderRegex = new Regex(Const.SENDER_REGEX, RegexOptions.Compiled);
        }

        public IEnumerable<Entity> GetAllAlliedStashesOnTerritory(Entity character)
        {
            var territoryIndex = Core.TerritoryService.GetTerritoryId(character);
            var serverGameManager = Core.ServerGameManager;
            var stashArray = stashQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var stash in stashArray)
                {
                    if (!serverGameManager.IsAllies(stash, character)) continue;
                    if (Core.TerritoryService.GetTerritoryId(stash) != territoryIndex) continue;

                    var name = stash.Read<NameableInteractable>().Name.ToString().ToLower();

                    yield return stash;
                }
            }
            finally
            {
                stashArray.Dispose();
            }
        }

        public IEnumerable<(int territoryIndex, int group, Entity station)> GetAllReceivingStashes(StashFilter filter = null)
        {
            foreach (var result in GetAllGroupStations(receiverRegex, filter))
            {
                yield return result;
            }
        }

        public IEnumerable<(int territoryIndex, int group, Entity station)> GetAllSendingStashes(StashFilter filter = null)
        {
            foreach (var result in GetAllGroupStations(senderRegex, filter))
            {
                yield return result;
            }
        }

        IEnumerable<(int territoryIndex, int group, Entity station)> GetAllGroupStations(Regex groupRegex, StashFilter filter = null)
        {
            var stashArray = stashQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var stash in stashArray)
                {
                    if (filter != null && !filter(stash))
                        continue;

                    var name = stash.Read<NameableInteractable>().Name.ToString().ToLower();
                    foreach (Match match in groupRegex.Matches(name))
                    {
                        var group = int.Parse(match.Groups[1].Value);
                        yield return (Core.TerritoryService.GetTerritoryId(stash), group, stash);
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
            var userEntity = charEntity.Read<PlayerCharacter>().UserEntity;
            var user = userEntity.Read<User>();
            ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "Stashing your inventory to storage on your current territory");

            var serverGameManager = Core.ServerGameManager;
            var matches = new Dictionary<PrefabGUID, List<(Entity stash, Entity inventory)>>(capacity: 100);
            try
            {
                foreach (var stash in GetAllAlliedStashesOnTerritory(charEntity))
                {
                    if (!serverGameManager.TryGetBuffer<AttachedBuffer>(stash, out var buffer))
                        continue;

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
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Exited UpdateRefiningSystem matchesProcessing early: {e}");
            }

            // get player inventory and find allied owned stashes in same territory with item matches
            if (!InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, charEntity, out Entity inventory))
                return;

            if (!serverGameManager.TryGetBuffer<InventoryBuffer>(inventory, out var inventoryBuffer))
                return;

            for (int i = ACTION_BAR_SLOTS; i < inventoryBuffer.Length; i++)
            {
                var item = inventoryBuffer[i].ItemType;
                if (!matches.TryGetValue(item, out var stashEntries)) continue;
                        
                foreach (var stashEntry in stashEntries)
                {
                    int transferAmount = serverGameManager.GetInventoryItemCount(inventory, item);
                    Utilities.TransferItems(serverGameManager, inventory, stashEntry.inventory, item, transferAmount);
                    ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user,
                        $"Stashed {transferAmount}x {item.DisplayName()} to {stashEntry.stash.Read<NameableInteractable>().Name}");
                }
            }
        }
    }
}
