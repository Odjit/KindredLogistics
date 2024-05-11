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
            var stationArray = stashQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var station in stationArray)
                {
                    if (filter != null && !filter(station))
                        continue;

                    var name = station.Read<NameableInteractable>().Name.ToString().ToLower();
                    foreach (Match match in groupRegex.Matches(name))
                    {
                        var group = int.Parse(match.Groups[1].Value);
                        yield return (Core.TerritoryService.GetTerritoryId(station), group, station);
                    }
                }
            }
            finally
            {
                stationArray.Dispose();
            }
        }

        public void StashCharacterInventory(Entity charEntity)
        {
            var userEntity = charEntity.Read<PlayerCharacter>().UserEntity;
            var user = userEntity.Read<User>();
            ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "Stashing your inventory to storage on your current territory");

            var serverGameManager = Core.ServerGameManager;
            var stashes = stashQuery.ToEntityArray(Allocator.TempJob);
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
                            else if (external.Entity.Read<PrefabGUID>().Equals(ExternalInventoryPrefab))
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
