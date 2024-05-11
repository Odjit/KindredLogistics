using BepInEx.Unity.IL2CPP.Utils.Collections;
using ProjectM;
using ProjectM.Network;
using ProjectM.Physics;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace KindredLogistics.Services
{
    internal class ConveyorService
    {
        readonly System.Random random = new();

        readonly List<Entity> distributionList = [];
        readonly Dictionary<Entity, int> amountReceiving = [];

        readonly IgnorePhysicsDebugSystem conveyorMonoBehaviour;

        const int MIN_TERRITORY_ID = 0;
        const int MAX_TERRITORY_ID = 138;

        public ConveyorService()
        {
            conveyorMonoBehaviour = (new GameObject("ConveyorService")).AddComponent<IgnorePhysicsDebugSystem>();
            conveyorMonoBehaviour.StartCoroutine(UpdateLoop().WrapToIl2Cpp());
        }

        IEnumerator UpdateLoop()
        {
            yield return null;
            while (true)
            {
                for (int i = MIN_TERRITORY_ID; i <= MAX_TERRITORY_ID; i++)
                {
                    ProcessConveyors(i);
                    yield return null;
                }
            }
        }

        void ProcessConveyors(int territoryId)
        {
            bool IsProcessingConveyor(Entity entity)
            {
                var owner = entity.Read<UserOwner>().Owner;
                if (owner.Equals(NetworkedEntity.Empty)) return false;

                if (!Core.PlayerSettings.IsConveyorEnabled(owner.GetEntityOnServer().Read<User>().PlatformId))
                    return false;

                return Core.TerritoryService.GetTerritoryId(entity) == territoryId;
            }

            var serverGameManager = Core.ServerGameManager;

            // Determine what is needed for each station
            var receivingNeeds = new Dictionary<(int territoryIndex, int group, PrefabGUID item), List<(Entity receiver, int amount)>>();
            foreach(var (territoryIndex, group, station) in Core.RefinementStations.GetAllReceivingStations(IsProcessingConveyor))
            {
                var receivingStation = station.Read<Refinementstation>();
                var inputInventoryEntity = receivingStation.InputInventoryEntity.GetEntityOnServer();
                var inventoryBuffer = inputInventoryEntity.ReadBuffer<InventoryBuffer>();
                var recipesBuffer = station.ReadBuffer<RefinementstationRecipesBuffer>();
                foreach (var recipe in recipesBuffer)
                {
                    if (recipe.Disabled) continue;

                    Entity recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[recipe.RecipeGuid];
                    var requirements = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
                    foreach (var requirement in requirements)
                    {
                        // Always desire 2x the amount so the moment it finishes it immediately starts again
                        var amountWanted = 2 * requirement.Amount;

                        // Check how much is already in the inventory
                        int has = 0;
                        foreach (var item in inventoryBuffer)
                        {
                            if (item.ItemType.Equals(requirement.Guid))
                            {
                                amountWanted -= item.Amount;
                                has = item.Amount;
                            }
                        }

                        if (amountWanted <= 0) continue;

                        if (!receivingNeeds.TryGetValue((territoryIndex, group, requirement.Guid), out var needs))
                        {
                            needs = [];
                            receivingNeeds[(territoryIndex, group, requirement.Guid)] = needs;
                        }

                        needs.Add((inputInventoryEntity, requirement.Amount));
                    }
                }
            }

            // Determine what is desired by each receiving stash
            foreach (var (territoryIndex, group, stash) in Core.Stash.GetAllReceivingStashes(IsProcessingConveyor))
            {
                if (!serverGameManager.TryGetBuffer<AttachedBuffer>(stash, out var buffer))
                    continue;
                foreach (var attachedBuffer in buffer)
                {
                    var attachedEntity = attachedBuffer.Entity;
                    if (!attachedEntity.Has<PrefabGUID>()) continue;
                    if (!attachedEntity.Read<PrefabGUID>().Equals(StashService.ExternalInventoryPrefab)) continue;

                    var inventoryBuffer = attachedEntity.ReadBuffer<InventoryBuffer>();
                    foreach (var item in inventoryBuffer)
                    {
                        if (item.ItemType.GuidHash == 0) continue;
                        if (!receivingNeeds.TryGetValue((territoryIndex, group, item.ItemType), out var needs))
                        {
                            needs = [];
                            receivingNeeds[(territoryIndex, group, item.ItemType)] = needs;
                        }

                        needs.Add((attachedEntity, 1));
                    }
                }
            }

            // Now distribute from all the sender stations to the stations in need
            foreach (var (territoryIndex, group, sendingStation) in Core.RefinementStations.GetAllSendingStations(IsProcessingConveyor))
            {
                var refinementStation = sendingStation.Read<Refinementstation>();
                var outputInventoryEntity = refinementStation.OutputInventoryEntity.GetEntityOnServer();
                if (outputInventoryEntity.Equals(Entity.Null)) continue;
                DistributeInventory(receivingNeeds, serverGameManager, territoryIndex, group, outputInventoryEntity);
            }

            // Next distribute from all the send stashes
            foreach (var (territoryIndex, group, sendingStash) in Core.Stash.GetAllSendingStashes(IsProcessingConveyor))
            {
                if (!serverGameManager.TryGetBuffer<AttachedBuffer>(sendingStash, out var buffer))
                    continue;
                foreach(var attachedBuffer in buffer)
                {
                    var attachedEntity = attachedBuffer.Entity;
                    if (!attachedEntity.Has<PrefabGUID>()) continue;
                    if (!attachedEntity.Read<PrefabGUID>().Equals(StashService.ExternalInventoryPrefab)) continue;

                    DistributeInventory(receivingNeeds, serverGameManager, territoryIndex, group, attachedEntity, retain: 1);
                }
            }
        }

        void DistributeInventory(Dictionary<(int territoryIndex, int group, PrefabGUID item), List<(Entity receiver, int amount)>> receivingNeeds,
                                 ServerGameManager serverGameManager, int territoryIndex, int group, Entity inventoryEntity, int retain=0)
        {
            var inventoryBuffer = inventoryEntity.ReadBuffer<InventoryBuffer>();
            foreach (var item in inventoryBuffer)
            {
                if (item.ItemType.GuidHash == 0) continue;
                // Does anyone need this item?
                if (!receivingNeeds.TryGetValue((territoryIndex, group, item.ItemType), out var needs)) continue;

                // Distribute the item to all the stations in need weighted by the amount needed
                var amount = item.Amount - retain;

                if (amount <= 0) continue;

                var totalWanted = needs.Sum(x => x.amount);

                // If we have more than enough, distribute evenly
                if (totalWanted < amount)
                {
                    foreach (var (receivingInventoryEntity, wanted) in needs)
                        Utilities.TransferItems(serverGameManager, inventoryEntity, receivingInventoryEntity, item.ItemType, wanted);
                    needs.Clear();
                }
                else
                {
                    // Can only give out whole numbers so need to randomly portion it out based on a weight of the desired amount
                    // Over time this should even out
                    distributionList.Clear();
                    foreach (var (receivingInventoryEntity, wanted) in needs)
                    {
                        for (int i = 0; i < wanted; i++)
                        {
                            distributionList.Add(receivingInventoryEntity);
                        }
                    }

                    amountReceiving.Clear();
                    for (int i = 0; i < amount; i++)
                    {
                        var index = random.Next(distributionList.Count);

                        // Determine who gets it
                        var receivingInventoryEntity = distributionList[index];
                        if (!amountReceiving.TryGetValue(receivingInventoryEntity, out var receivingAmount))
                            amountReceiving[receivingInventoryEntity] = 1;
                        else
                            amountReceiving[receivingInventoryEntity] = receivingAmount + 1;
                    }

                    foreach (var (receivingInventoryEntity, receivingAmount) in amountReceiving)
                    {
                        Utilities.TransferItems(serverGameManager, inventoryEntity, receivingInventoryEntity, item.ItemType, receivingAmount);

                        // Remove the amount from the needs list
                        var amountToRemove = receivingAmount;
                        for (int i = needs.Count - 1; i >= 0; i--)
                        {
                            if (!needs[i].receiver.Equals(receivingInventoryEntity)) continue;

                            if (needs[i].amount > amountToRemove)
                            {
                                needs[i] = (receivingInventoryEntity, needs[i].amount - amountToRemove);
                                break;
                            }
                            else
                            {
                                amountToRemove -= needs[i].amount;
                                needs.RemoveAt(i);
                            }
                        }
                    }
                }
            }
        }
    }
}
