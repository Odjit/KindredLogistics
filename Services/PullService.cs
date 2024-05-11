using KindredLogistics.Patches;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System;
using Unity.Collections;
using Unity.Entities;

namespace KindredLogistics.Services
{
    internal class PullService
    {
        public static void HandleRecipePull(Entity character, PrefabGUID recipe)
        {
            var user = character.Read<PlayerCharacter>().UserEntity.Read<User>();
            var entityManager = Core.EntityManager;
            var serverGameManager = Core.ServerGameManager;
            var recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[recipe];


            var recipeName = recipeEntity.Read<PrefabGUID>().DisplayName();
            var recipeOutputBuffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
            if (recipeOutputBuffer.Length > 0)
            {
                var recipeOutput = recipeOutputBuffer[0];
                recipeName = recipeOutput.Guid.DisplayName();
            }

            ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Fetching crafting materials for {recipeName}.");

            var requirements = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
            var stashes = UpdateRefiningSystemPatch.stashesQuery.ToEntityArray(Allocator.TempJob);
            try
            {
                if (!InventoryUtilities.TryGetInventoryEntity(entityManager, character, out Entity inventory))
                {
                    Core.Log.LogWarning($"No inventory found for character {character}.");
                    return;
                }

                foreach (var requirement in requirements)
                {
                    var currentAmount = serverGameManager.GetInventoryItemCount(inventory, requirement.Guid);
                    if (currentAmount >= requirement.Amount) continue;

                    var requiredAmount = requirement.Amount - currentAmount;

                    foreach (var stash in Core.Stash.GetAllAlliedStashesOnTerritory(character))
                    {
                        if (requiredAmount <= 0) break;
                        if (!serverGameManager.TryGetBuffer<AttachedBuffer>(stash, out var buffer))
                            continue;
                        foreach (var attachedBuffer in buffer)
                        {
                            var attachedEntity = attachedBuffer.Entity;
                            if (!attachedEntity.Has<PrefabGUID>()) continue;
                            if (!attachedEntity.Read<PrefabGUID>().Equals(StashService.ExternalInventoryPrefab)) continue;

                            var stashItemCount = serverGameManager.GetInventoryItemCount(attachedEntity, requirement.Guid);
                            if (stashItemCount >= requiredAmount)
                            {
                                Utilities.TransferItems(serverGameManager, attachedEntity, inventory, requirement.Guid, requiredAmount);
                                requiredAmount = 0;
                                break;
                            }
                            else
                            {
                                Utilities.TransferItems(serverGameManager, attachedEntity, inventory, requirement.Guid, stashItemCount);
                                requiredAmount -= stashItemCount;
                            }
                        }
                    }

                    if (requiredAmount > 0)
                    {
                        ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Couldn't find {requiredAmount}x {requirement.Guid.DisplayName()}.");
                    }
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
