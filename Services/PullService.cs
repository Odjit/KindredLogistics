using KindredLogistics.Patches;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KindredLogistics.Services
{
    internal class PullService
    {
        public static int PullItem(Entity character, PrefabGUID item, int quantity)
        {
            var entityManager = Core.EntityManager;
            var serverGameManager = Core.ServerGameManager;

            if (!InventoryUtilities.TryGetInventoryEntity(entityManager, character, out Entity inventory))
            {
                Core.Log.LogWarning($"No inventory found for character {character}.");
                return quantity;
            }

            var quantityRemaining = quantity;
            foreach (var stash in Core.Stash.GetAllAlliedStashesOnTerritory(character))
            {
                if (quantityRemaining <= 0) break;
                if (!serverGameManager.TryGetBuffer<AttachedBuffer>(stash, out var buffer))
                    continue;
                foreach (var attachedBuffer in buffer)
                {
                    var attachedEntity = attachedBuffer.Entity;
                    if (!attachedEntity.Has<PrefabGUID>()) continue;
                    if (!attachedEntity.Read<PrefabGUID>().Equals(StashService.ExternalInventoryPrefab)) continue;

                    var stashItemCount = serverGameManager.GetInventoryItemCount(attachedEntity, item);
                    if (stashItemCount >= quantityRemaining)
                    {
                        Utilities.TransferItems(serverGameManager, attachedEntity, inventory, item, quantityRemaining);
                        quantityRemaining = 0;
                        break;
                    }
                    else
                    {
                        Utilities.TransferItems(serverGameManager, attachedEntity, inventory, item, stashItemCount);
                        quantityRemaining -= stashItemCount;
                    }
                }
            }

            return quantityRemaining;
        }

        public static void HandleRecipePull(Entity character, Entity workstation, PrefabGUID recipe)
        {
            var user = character.Read<PlayerCharacter>().UserEntity.Read<User>();
            var entityManager = Core.EntityManager;
            var serverGameManager = Core.ServerGameManager;
            var recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[recipe];

            var recipeName = recipeEntity.Read<PrefabGUID>().LookupName();
            var recipeOutputBuffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
            if (recipeOutputBuffer.Length > 0)
            {
                var recipeOutput = recipeOutputBuffer[0];
                recipeName = recipeOutput.Guid.PrefabName();
            }

            var castleWorkstation = workstation.Read<CastleWorkstation>();
            var recipeReduction = castleWorkstation.WorkstationLevel.HasFlag(WorkstationLevel.MatchingFloor) ? 0.75f : 1f;

            var requirements = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
            var stashes = UpdateRefiningSystemPatch.stashesQuery.ToEntityArray(Allocator.TempJob);
            try
            {
                if (!InventoryUtilities.TryGetInventoryEntity(entityManager, character, out Entity inventory))
                {
                    Core.Log.LogWarning($"No inventory found for character {character}.");
                    return;
                }

                var workstationInventory = Entity.Null;
                if (serverGameManager.TryGetBuffer<AttachedBuffer>(workstation, out var workStationBuffer))
                {
                    foreach (var attachedBuffer in workStationBuffer)
                    {
                        var attachedEntity = attachedBuffer.Entity;
                        if (!attachedEntity.Has<PrefabGUID>()) continue;
                        if (!attachedEntity.Read<PrefabGUID>().Equals(StashService.ExternalInventoryPrefab)) continue;

                        workstationInventory = attachedEntity;
                        break;
                    }
                }

                var fetchedMaterials = false;
                foreach (var requirement in requirements)
                {
                    var currentAmount = serverGameManager.GetInventoryItemCount(inventory, requirement.Guid);
                    if (!workstationInventory.Equals(Entity.Null))
                        currentAmount += serverGameManager.GetInventoryItemCount(workstationInventory, requirement.Guid);
                    var requiredAmount = Mathf.RoundToInt(requirement.Amount * recipeReduction);
                    if (currentAmount >= requiredAmount) continue;

                    if (!fetchedMaterials)
                    {
                        fetchedMaterials = true;
                        ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Fetching crafting materials for {recipeName}.");
                    }

                    requiredAmount -= currentAmount;

                    foreach (var stash in Core.Stash.GetAllAlliedStashesOnTerritory(character))
                    {
                        if (requiredAmount <= 0) break;
                        if (stash.Equals(workstation)) continue;
                        if (!serverGameManager.TryGetBuffer<AttachedBuffer>(stash, out var buffer))
                            continue;
                        foreach (var attachedBuffer in buffer)
                        {
                            var attachedEntity = attachedBuffer.Entity;
                            if (!attachedEntity.Has<PrefabGUID>()) continue;
                            if (!attachedEntity.Read<PrefabGUID>().Equals(StashService.ExternalInventoryPrefab)) continue;

                            var stashItemCount = serverGameManager.GetInventoryItemCount(attachedEntity, requirement.Guid);

                            if (stashItemCount == 0) continue;
                            var transferAmount = Mathf.Min(stashItemCount, requiredAmount);
                            Utilities.TransferItems(serverGameManager, attachedEntity, inventory, requirement.Guid, transferAmount);
                            ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"{transferAmount}x {requirement.Guid.PrefabName()} fetched from {stash.EntityName()}.");
                            
                            requiredAmount -= transferAmount;
                            if (requiredAmount <= 0)
                                break;
                        }
                    }

                    if (requiredAmount > 0)
                    {
                        ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Couldn't find {requiredAmount}x {requirement.Guid.PrefabName()}.");
                    }
                }
                if (!fetchedMaterials)
                {
                    ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Already had materials for crafting {recipeName}.");
                }
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Error in HandleRecipePull: {e}");
            }
            finally
            {
                stashes.Dispose();
            }
        }
    }
}
