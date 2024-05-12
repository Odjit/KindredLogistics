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
            var user = character.Read<PlayerCharacter>().UserEntity.Read<User>();
            var entityManager = Core.EntityManager;
            var serverGameManager = Core.ServerGameManager;

            if (!InventoryUtilities.TryGetInventoryEntity(entityManager, character, out Entity inventory))
            {
                Core.Log.LogWarning($"No inventory found for character {character}.");
                return quantity;
            }

            var dontPullLast = Core.PlayerSettings.IsDontPullLastEnabled(user.PlatformId);

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
                    if (dontPullLast)
                        stashItemCount -= 1;

                    if (stashItemCount <= 0) continue;

                    var transferAmount = Mathf.Min(stashItemCount, quantityRemaining);
                    Utilities.TransferItems(serverGameManager, attachedEntity, inventory, item, transferAmount);
                    ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"<color=white>{transferAmount}</color>x <color=green>{item.PrefabName()}</color> fetched from <color=#FFC0CB>{stash.EntityName()}</color>");
                    quantityRemaining -= transferAmount;
                    if(quantityRemaining <= 0)
                        break;
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

            var dontPullLast = Core.PlayerSettings.IsDontPullLastEnabled(user.PlatformId);

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
                        ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Fetching materials for <color=yellow>{recipeName}</color>...");
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

                            if (dontPullLast)
                                stashItemCount -= 1;

                            if (stashItemCount <= 0) continue;

                            var transferAmount = Mathf.Min(stashItemCount, requiredAmount);
                            Utilities.TransferItems(serverGameManager, attachedEntity, inventory, requirement.Guid, transferAmount);
                            ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"<color=white>{transferAmount}</color>x <color=green>{requirement.Guid.PrefabName()}</color> fetched from <color=#FFC0CB>{stash.EntityName()}</color>");
                            requiredAmount -= transferAmount;
                            if (requiredAmount <= 0)
                                break;
                        }
                    }

                    if (requiredAmount > 0)
                    {
                        ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Couldn't find <color=white>{requiredAmount}</color>x <color=green>{requirement.Guid.PrefabName()}</color> for recipe...");
                    }
                }
                if (!fetchedMaterials)
                {
                    ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Already have materials for crafting <color=yellow>{recipeName}</color>!");
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
