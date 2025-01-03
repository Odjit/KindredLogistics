using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using System;
using Unity.Entities;
using UnityEngine;

namespace KindredLogistics.Services
{
    internal class PullService
    {
        public static void PullItem(Entity character, PrefabGUID item, int quantity)
        {
            var user = character.Read<PlayerCharacter>().UserEntity.Read<User>();
            if (Core.PlayerSettings.IsPullEnabled())
            { 
                ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "Pulling is globally disabled.");
                return;
            }

            if(!Core.GameDataSystem.ItemHashLookupMap.TryGetValue(item, out var itemData))
            {
                ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "Invalid item specified.");
                return;
            }

            var entityManager = Core.EntityManager;
            var serverGameManager = Core.ServerGameManager;
            var territoryIndex = Core.TerritoryService.GetTerritoryId(character);
           
            if (territoryIndex == -1)
            {
                ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "Unable to pull outside territories!");
                return;
            }
            if (!InventoryUtilities.TryGetInventoryEntity(entityManager, character, out Entity inventory))
            {
                Core.Log.LogWarning($"No inventory found for character {character}.");
                return;
            }
            var batform = new PrefabGUID(1205505492);
            if (BuffUtility.TryGetBuff(Core.EntityManager, character, batform , out var _))
            {
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, "Cannot pull items while in batform.");
                return;
            }

            var isAnItemEntity = !itemData.Entity.Equals(Entity.Null);

            var dontPullLast = Core.PlayerSettings.IsDontPullLastEnabled(user.PlatformId);
            var silentPull = Core.PlayerSettings.IsSilentPullEnabled(user.PlatformId);

            var quantityRemaining = quantity;
            var foundStash = false;
            var playerInventorySlot = 0;
            var inventoryFull = false;
            foreach (var stash in Core.Stash.GetAllAlliedStashesOnTerritory(character))
            {
                if (quantityRemaining <= 0) break;
                if (!serverGameManager.TryGetBuffer<AttachedBuffer>(stash, out var buffer))
                    continue;

                foundStash = true;

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

                    if (isAnItemEntity)
                    {
                        if (Utilities.TransferItemEntities(attachedEntity, inventory, item, transferAmount, ref playerInventorySlot, out transferAmount))
                        {
                            inventoryFull = true;
                            break;
                        }
                    }
                    else
                    {
                        transferAmount = Utilities.TransferItems(serverGameManager, attachedEntity, inventory, item, transferAmount);
                    }
                    if (transferAmount <= 0)
                    {
                        if (inventoryFull)
                            break;
                        continue;
                    }
                    if (!silentPull)
                        ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"<color=white>{transferAmount}</color>x <color=green>{item.PrefabName()}</color> fetched from <color=#FFC0CB>{stash.EntityName()}</color>");
                    quantityRemaining -= transferAmount;
                    if (quantityRemaining <= 0 || inventoryFull)
                        break;
                }
            }

            if (!foundStash)
                ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "Unable to pull as no available stashes found in your current territory!");
            else if (quantityRemaining <= 0)
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Pulled {quantity}x {item.PrefabName()} from containers.");
            else
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Was able to only pull {quantity - quantityRemaining}x out of desired {quantity}x {item.PrefabName()} from containers.");

            if (inventoryFull)
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, "Inventory is full, unable to pull more items.");

        }

        public static void HandleRecipePull(Entity character, Entity workstation, PrefabGUID recipe)
        {
            var user = character.Read<PlayerCharacter>().UserEntity.Read<User>();
            var entityManager = Core.EntityManager;
            if (!InventoryUtilities.TryGetInventoryEntity(entityManager, character, out Entity inventory))
            {
                Core.Log.LogWarning($"No inventory found for character {character}.");
                return;
            }

            var serverGameManager = Core.ServerGameManager;
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

            // Determine the multiple of the recipe we currently have then we will try to fetch up to one more recipe's worth of materials
            var currentRecipeMultiple = -1;
            var castleWorkstation = workstation.Read<CastleWorkstation>();
            var recipeReduction = castleWorkstation.WorkstationLevel.HasFlag(WorkstationLevel.MatchingFloor) ? 0.75 : 1;
            var recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[recipe];
            var requirements = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
            foreach (var requirement in requirements)
            {
                var currentAmount = serverGameManager.GetInventoryItemCount(inventory, requirement.Guid);
                if (!workstationInventory.Equals(Entity.Null))
                    currentAmount += serverGameManager.GetInventoryItemCount(workstationInventory, requirement.Guid);
                var requiredAmount = (int)Math.Round(requirement.Amount * recipeReduction, MidpointRounding.ToPositiveInfinity);

                var itemRecipeMultiple = currentAmount / requiredAmount;
                if (currentRecipeMultiple < 0)
                    currentRecipeMultiple = itemRecipeMultiple;
                else
                    currentRecipeMultiple = Mathf.Min(currentRecipeMultiple, itemRecipeMultiple);
            }

            var recipeName = recipeEntity.Read<PrefabGUID>().LookupName();
            var recipeOutputBuffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
            if (recipeOutputBuffer.Length > 0)
            {
                var recipeOutput = recipeOutputBuffer[0];
                recipeName = recipeOutput.Guid.PrefabName();
            }

            var fetchedForAnother = true;
            var fetchedMaterials = false;
            var desiredRecipeMultiple = currentRecipeMultiple + 1;
            var dontPullLast = Core.PlayerSettings.IsDontPullLastEnabled(user.PlatformId);
            var silentPull = Core.PlayerSettings.IsSilentPullEnabled(user.PlatformId);
            foreach (var requirement in requirements)
            {
                RetrieveRequirement(character, workstation, user, entityManager, ref serverGameManager, recipeName, dontPullLast, silentPull, inventory,
                    workstationInventory, ref fetchedForAnother, ref fetchedMaterials, requirement.Guid, requirement.Amount, desiredRecipeMultiple,
                    recipeReduction, "crafting");
            }
            if (!fetchedMaterials)
            {
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Couldn't find any materials for crafting additional <color=yellow>{recipeName}</color>!");
            }
            ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Have enough materials for crafting <color=white>{(fetchedForAnother ? desiredRecipeMultiple : currentRecipeMultiple)}</color>x <color=yellow>{recipeName}</color>.");
        }

        public static void HandleRepairPull(Entity character, PrefabGUID recipe, float repairNeeded)
        {
            var user = character.Read<PlayerCharacter>().UserEntity.Read<User>();
            var entityManager = Core.EntityManager;

            if (Core.PlayerSettings.IsPullEnabled())
            {
                ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "Pulling is globally disabled.");
                return;
            }

            var territoryIndex = Core.TerritoryService.GetTerritoryId(character);

            if (territoryIndex == -1)
            {
                ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, "Unable to pull outside territories!");
                return;
            }

            if (!InventoryUtilities.TryGetInventoryEntity(entityManager, character, out Entity inventory))
            {
                Core.Log.LogWarning($"No inventory found for character {character}.");
                return;
            }

            var serverGameManager = Core.ServerGameManager;
           

            // Determine the multiple of the recipe we currently have then we will try to fetch up to one more recipe's worth of materials
            var recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[recipe];
            if (!recipeEntity.Has<ItemRepairBuffer>())
            {
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, "Invalid recipe specified.");
                return;
            }
            var requirements = recipeEntity.ReadBuffer<ItemRepairBuffer>();

            var recipeName = recipeEntity.Read<PrefabGUID>().LookupName();

            var recipeOutputBuffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
            if (recipeOutputBuffer.Length > 0)
            {
                var recipeOutput = recipeOutputBuffer[0];
                recipeName = recipeOutput.Guid.PrefabName();
            }

            var fetchedForAnother = true;
            var fetchedMaterials = false;
            var desiredRecipeMultiple = 1;
            var dontPullLast = Core.PlayerSettings.IsDontPullLastEnabled(user.PlatformId);
            var silentPull = Core.PlayerSettings.IsSilentPullEnabled(user.PlatformId);

            foreach (var requirement in requirements)
            {
                int repairAmount = (int)Math.Ceiling(requirement.Stacks * (1 - repairNeeded));
                RetrieveRequirement(character, Entity.Null, user, entityManager, ref serverGameManager, recipeName, dontPullLast, silentPull, inventory,
                    inventory, ref fetchedForAnother, ref fetchedMaterials, requirement.Guid, repairAmount, desiredRecipeMultiple,
                    1, "repairing");
            }
        }
        public static void HandleForgePull(Entity character, Entity workstation, Entity item)
        {
            var user = character.Read<PlayerCharacter>().UserEntity.Read<User>();
            var entityManager = Core.EntityManager;
            if (!InventoryUtilities.TryGetInventoryEntity(entityManager, character, out Entity inventory))
            {
                Core.Log.LogWarning($"No inventory found for character {character}.");
                return;
            }

            var serverGameManager = Core.ServerGameManager;
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

            // Determine the multiple of the recipe we currently have then we will try to fetch up to one more recipe's worth of materials
            var currentRecipeMultiple = -1;
            var castleWorkstation = workstation.Read<CastleWorkstation>();
            var recipeReduction = castleWorkstation.WorkstationLevel.HasFlag(WorkstationLevel.MatchingFloor) ? 0.75f : 1f;
            var requirements = item.ReadBuffer<ShatteredItemRepairCost>();
            foreach (var requirement in requirements)
            {
                var currentAmount = serverGameManager.GetInventoryItemCount(inventory, requirement.ItemId);
                if (!workstationInventory.Equals(Entity.Null))
                    currentAmount += serverGameManager.GetInventoryItemCount(workstationInventory, requirement.ItemId);
                var requiredAmount = (int)Math.Round(requirement.Amount * recipeReduction, MidpointRounding.ToPositiveInfinity);

                var itemRecipeMultiple = currentAmount / requiredAmount;
                if (currentRecipeMultiple < 0)
                    currentRecipeMultiple = itemRecipeMultiple;
                else
                    currentRecipeMultiple = Mathf.Min(currentRecipeMultiple, itemRecipeMultiple);
            }

            var desiredRecipeMultiple = currentRecipeMultiple + 1;

            var fetchedForAnother = true;
            var fetchedMaterials = false;
            var recipeName = item.Read<PrefabGUID>().PrefabName();
            var dontPullLast = Core.PlayerSettings.IsDontPullLastEnabled(user.PlatformId);
            var silentPull = Core.PlayerSettings.IsSilentPullEnabled(user.PlatformId);
            foreach (var requirement in requirements)
            {
                RetrieveRequirement(character, workstation, user, entityManager, ref serverGameManager, recipeName, dontPullLast, silentPull, inventory,
                    workstationInventory, ref fetchedForAnother, ref fetchedMaterials, requirement.ItemId, requirement.Amount, desiredRecipeMultiple, recipeReduction, "forging");
            }
            if (!fetchedMaterials)
            {
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Couldn't find any materials for forging additional <color=yellow>{recipeName}</color>!");
            }
            ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Have enough materials for forging <color=white>{(fetchedForAnother ? desiredRecipeMultiple : currentRecipeMultiple)}</color>x <color=yellow>{recipeName}</color>.");
        }

        public static void HandleForgeUpgradePull(Entity character, Entity workstation, Entity item)
        {
            var user = character.Read<PlayerCharacter>().UserEntity.Read<User>();
            var entityManager = Core.EntityManager;
            if (!InventoryUtilities.TryGetInventoryEntity(entityManager, character, out Entity inventory))
            {
                Core.Log.LogWarning($"No inventory found for character {character}.");
                return;
            }

            var serverGameManager = Core.ServerGameManager;
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

            // Determine the multiple of the recipe we currently have then we will try to fetch up to one more recipe's worth of materials
            var requirements = item.ReadBuffer<UpgradeableLegendaryItemTiers>();
            var upgradeableLegendaryItem = item.Read<UpgradeableLegendaryItem>();
            var requirement = requirements[upgradeableLegendaryItem.NextTier];
            var currentRecipeMultiple = serverGameManager.GetInventoryItemCount(inventory, requirement.TierPrefab);
            if (!workstationInventory.Equals(Entity.Null))
                currentRecipeMultiple += serverGameManager.GetInventoryItemCount(workstationInventory, requirement.TierPrefab);

            var fetchedForAnother = true;
            var fetchedMaterials = false;
            var desiredRecipeMultiple = currentRecipeMultiple + 1;
            var recipeName = item.Read<PrefabGUID>().PrefabName();
            var dontPullLast = Core.PlayerSettings.IsDontPullLastEnabled(user.PlatformId);
            var silentPull = Core.PlayerSettings.IsSilentPullEnabled(user.PlatformId);
            RetrieveRequirement(character, workstation, user, entityManager, ref serverGameManager, recipeName, dontPullLast, silentPull, inventory,
                        workstationInventory, ref fetchedForAnother, ref fetchedMaterials, requirement.TierPrefab, 1, desiredRecipeMultiple, 1, "upgrading");

            if (!fetchedMaterials)
            {
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Couldn't find any materials for upgrading additional <color=yellow>{recipeName}</color>!");
            }
            ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Have enough materials for upgrading <color=white>{(fetchedForAnother ? desiredRecipeMultiple : currentRecipeMultiple)}</color>x <color=yellow>{recipeName}</color>.");
        }

        static void RetrieveRequirement(Entity character, Entity workstation, User user, EntityManager entityManager, ref ServerGameManager serverGameManager,
                                                string recipeName, bool dontPullLast, bool silentPull, Entity inventory, Entity workstationInventory, ref bool fetchedForAnother,
                                                ref bool fetchedMaterials, PrefabGUID requiredItem, int requiredAmount, int desiredRecipeMultiple, double recipeReduction,
                                                string fetchMessage = "")
        {
            var currentAmount = serverGameManager.GetInventoryItemCount(inventory, requiredItem);
            if (!workstationInventory.Equals(Entity.Null))
                currentAmount += serverGameManager.GetInventoryItemCount(workstationInventory, requiredItem);
            requiredAmount = desiredRecipeMultiple * (int)Math.Round(requiredAmount * recipeReduction, MidpointRounding.ToPositiveInfinity);
            if (currentAmount >= requiredAmount) return;

            if (!fetchedMaterials)
            {
                fetchedMaterials = true;
                if (!silentPull)
                    ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Fetching materials for {fetchMessage} <color=yellow>{recipeName}</color>...");
            }

            requiredAmount -= currentAmount;

            var isAnItemEntity = Core.GameDataSystem.ItemHashLookupMap.TryGetValue(requiredItem, out var itemData) && !itemData.Entity.Equals(Entity.Null);
            var destinationSlot = 0;
            var isInventoryFull = false;

            foreach (var stash in Core.Stash.GetAllAlliedStashesOnTerritory(character))
            {
                if (isInventoryFull) break;
                if (requiredAmount <= 0) break;
                if (stash.Equals(workstation)) continue;
                if (!serverGameManager.TryGetBuffer<AttachedBuffer>(stash, out var buffer))
                    continue;

                // Don't pull resources that are being actively used from a spawner station
                if (stash.Has<UnitSpawnerstation>())
                {
                    var spawnerStation = stash.Read<UnitSpawnerstation>();
                    if (spawnerStation.IsWorking)
                    {
                        Entity recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[spawnerStation.CurrentRecipeGuid];
                        var requirements = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
                        var foundItem = false;
                        foreach (var requirement in requirements)
                        {
                            if (requirement.Guid.Equals(requiredItem))
                            {
                                foundItem = true;
                                break;
                            }
                        }
                        if (foundItem)
                            continue;
                    }
                }

                // Don't pull resources that are being actively used from a refinement station
                if (stash.Has<Refinementstation>())
                {
                    var refinementStation = stash.Read<Refinementstation>();
                    if (refinementStation.IsWorking)
                    {
                        Entity recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[refinementStation.CurrentRecipeGuid];
                        var requirements = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
                        var foundItem = false;
                        foreach (var requirement in requirements)
                        {
                            if (requirement.Guid.Equals(requiredItem))
                            {
                                foundItem = true;
                                break;
                            }
                        }
                        if (foundItem)
                            continue;
                    }
                }

                foreach (var attachedBuffer in buffer)
                {
                    var attachedEntity = attachedBuffer.Entity;
                    if (!attachedEntity.Has<PrefabGUID>()) continue;
                    if (!attachedEntity.Read<PrefabGUID>().Equals(StashService.ExternalInventoryPrefab)) continue;

                    var stashItemCount = serverGameManager.GetInventoryItemCount(attachedEntity, requiredItem);

                    if (dontPullLast)
                        stashItemCount -= 1;

                    if (stashItemCount <= 0) continue;

                    var transferAmount = Mathf.Min(stashItemCount, requiredAmount);
                    if (isAnItemEntity)
                    {
                        if (Utilities.TransferItemEntities(attachedEntity, inventory, requiredItem, transferAmount, ref destinationSlot, out transferAmount))
                        {
                            isInventoryFull = true;
                            break;
                        }
                    }
                    else
                    {
                        transferAmount = Utilities.TransferItems(serverGameManager, attachedEntity, inventory, requiredItem, transferAmount);
                    }
                    if (transferAmount <= 0)
                        continue;
                    
                    if (!silentPull)
                        ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"<color=white>{transferAmount}</color>x <color=green>{requiredItem.PrefabName()}</color> fetched from <color=#FFC0CB>{stash.EntityName()}</color>");
                    requiredAmount -= transferAmount;
                    if (requiredAmount <= 0)
                        break;
                }
            }

            if (requiredAmount > 0)
            {
                fetchedForAnother = false;
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Couldn't find <color=white>{requiredAmount}</color>x <color=green>{requiredItem.PrefabName()}</color>");
            }
        }
    }
}