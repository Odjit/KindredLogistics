using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System;
using Unity.Collections;
using Unity.Entities;

namespace KindredLogistics.Patches;

public class CraftingPatch
{
    [HarmonyPatch(typeof(StopCraftingSystem), nameof(StopCraftingSystem.OnUpdate))]
    public static class StopCraftingSystemPatch
    {
        public static void Prefix(StopCraftingSystem __instance)
        {
            var entities = __instance._EventQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Has<StopCraftItemEvent>() && entity.Has<FromCharacter>())
                    {
                        var stopCraftEvent = entity.Read<StopCraftItemEvent>();
                        var fromCharacter = entity.Read<FromCharacter>();
                        ulong steamId = fromCharacter.User.Read<User>().PlatformId;

                        if (!Core.PlayerSettings.IsCraftPullEnabled(steamId)) continue;
                        HandleRecipePull(fromCharacter.Character, stopCraftEvent.RecipeGuid);
                    }
                    else
                    {
                        Core.Log.LogInfo("Entity missing StopCraftItemEvent or FromCharacter components...");
                    }
                }
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Exited StopCraftingSystem hook early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }

        static void HandleRecipePull(Entity character, PrefabGUID recipe)
        {
            var entityManager = Core.EntityManager;
            var serverGameManager = Core.ServerGameManager;
            var recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[recipe];
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
                    Core.Log.LogInfo($"Searching stashes for pull: {requiredAmount} {requirement.Guid.LookupName()} for {recipe.LookupName()}...");

                    foreach (var stash in stashes)
                    {
                        // Check if stash is allied and in the same territory
                        if (!serverGameManager.IsAllies(stash, character) || !Utilities.TerritoryCheck(character, stash)) continue;

                        if (!serverGameManager.TryGetBuffer<AttachedBuffer>(stash, out var buffer))
                        {
                            Core.Log.LogInfo("No AttachedBuffer found for entity.");
                            continue;
                        }

                        var externalInventory = Entity.Null;
                        foreach (var external in buffer)
                        {
                            if (external.Entity.Read<PrefabGUID>().Equals(UpdateRefiningSystemPatch.externalInventoryPrefab))
                            {
                                externalInventory = external.Entity;
                                break;
                            }
                        }
                        if (externalInventory.Equals(Entity.Null)) continue;

                        var stashItemCount = serverGameManager.GetInventoryItemCount(externalInventory, requirement.Guid);
                        if (stashItemCount >= requiredAmount)
                        {
                            Utilities.TransferItems(serverGameManager, externalInventory, inventory, requirement.Guid, requiredAmount);
                        }
                        else
                        {
                            Core.Log.LogInfo($"Not enough {requirement.Guid.LookupName()} to pull for recipe...");
                        }
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