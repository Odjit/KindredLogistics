using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Network;
using Stunlock.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;

namespace KindredLogistics.Services
{
    internal class StashService
    {
        private const int ACTION_BAR_SLOTS = 8;
        private const string SKIP_SUFFIX = "''";
        private const float FIND_SPOTLIGHT_DURATION = 15f;

        private static readonly ComponentType[] StashQuery =
            [
                ComponentType.ReadOnly(Il2CppType.Of<InventoryOwner>()),
                ComponentType.ReadOnly(Il2CppType.Of<CastleHeartConnection>()),
                ComponentType.ReadOnly(Il2CppType.Of<AttachedBuffer>()),
                ComponentType.ReadOnly(Il2CppType.Of<NameableInteractable>()),
            ];

        public static readonly PrefabGUID ExternalInventoryPrefab = new(1183666186);
        private static readonly PrefabGUID findContainerSpotlightPrefab = new(-1466712470);

        public delegate bool StashFilter(Entity station);

        private EntityQuery stashQuery;
        private readonly Regex receiverRegex;
        private readonly Regex senderRegex;

        private Dictionary<Entity, (double expirationTime, List<Entity> targetStashes)> activeSpotlights = [];

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

                    var name = stash.Read<NameableInteractable>().Name.ToString();
                    if (name.EndsWith(SKIP_SUFFIX)) continue;

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

        private IEnumerable<(int territoryIndex, int group, Entity station)> GetAllGroupStations(Regex groupRegex, StashFilter filter = null)
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

            var territoryIndex = Core.TerritoryService.GetTerritoryId(charEntity);
            if (territoryIndex == -1)
            {
                ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "Unable to stash outside territories!");
                return;
            }

            ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "Stashing inventory to storage in your current territory...");

            var serverGameManager = Core.ServerGameManager;
            var matches = new Dictionary<PrefabGUID, List<(Entity stash, Entity inventory)>>(capacity: 100);
            var foundStash = false;
            try
            {
                foreach (var stash in GetAllAlliedStashesOnTerritory(charEntity))
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
            }
            catch (System.Exception e)
            {
                Core.Log.LogError($"Exited UpdateRefiningSystem matchesProcessing early: {e}");
            }

            if (!foundStash)
            {
                ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "No available stashes found in your current territory!");
                return;
            }

            // get player inventory and find allied owned stashes in same territory with item matches
            if (!InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, charEntity, out Entity inventory))
                return;

            if (!serverGameManager.TryGetBuffer<InventoryBuffer>(inventory, out var inventoryBuffer))
                return;

            
            for (int i = ACTION_BAR_SLOTS; i < inventoryBuffer.Length; i++)
            {
                bool transferFlag = false;
                var item = inventoryBuffer[i].ItemType;
                if (!matches.TryGetValue(item, out var stashEntries)) continue;


                foreach (var stashEntry in stashEntries)
                {
                    int transferAmount = serverGameManager.GetInventoryItemCount(inventory, item);
                    if (transferAmount == 0) break;
                    transferAmount = Utilities.TransferItems(serverGameManager, inventory, stashEntry.inventory, item, transferAmount);
                    if (transferAmount > 0)
                    {
                        transferFlag = true;
                        ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user,
                            $"Stashed <color=white>{transferAmount}</color>x <color=green>{item.PrefabName()}</color> to <color=#FFC0CB>{stashEntry.stash.EntityName()}</color>");
                    }
                }
                
                int remainingAmount = serverGameManager.GetInventoryItemCount(inventory, item);
                if (remainingAmount > 0 && transferFlag)
                {
                    ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user,
                                               $"Unable to stash <color=white>{remainingAmount}</color>x <color=green>{item.PrefabName()}</color> due to insufficient space in stashes!");
                }
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
                ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "Unable to search for items outside allied territories!");
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

        private void ClearSpotlights(Entity userEntity)
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

        private void AddSpotlight(Entity stash, Entity userEntity)
        {
            if (!activeSpotlights.TryGetValue(userEntity, out var spotlight))
            {
                spotlight.expirationTime = Core.ServerTime + FIND_SPOTLIGHT_DURATION;
                spotlight.targetStashes = [];
                activeSpotlights.Add(userEntity, spotlight);
            }
            spotlight.targetStashes.Add(stash);

            Buffs.RemoveAndAddBuff(userEntity, stash, findContainerSpotlightPrefab, FIND_SPOTLIGHT_DURATION);
        }
    }
}