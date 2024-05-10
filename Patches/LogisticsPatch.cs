using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace KindredLogistics.Patches;

[HarmonyPatch(typeof(UpdateRefiningSystem), nameof(UpdateRefiningSystem.OnUpdate))]
public static class UpdateRefiningSystemPatch
{
    public static EntityQuery stationsQuery;
    public static EntityQuery stashesQuery;
    public static readonly PrefabGUID externalInventoryPrefab = new(1183666186);

    static UpdateRefiningSystemPatch()
    {
        stationsQuery = Core.EntityManager.CreateEntityQuery(Utilities.RefinementStationQuery);
        stashesQuery = Core.EntityManager.CreateEntityQuery(Utilities.StashQuery);
    }

    public static void Prefix(UpdateRefiningSystem __instance)
    {
        //Core.Log.LogInfo("Running UpdateRefiningSystem hook...");
        NativeArray<Entity> stations = stationsQuery.ToEntityArray(Allocator.TempJob);
        var needs = new Dictionary<PrefabGUID, List<(Entity station, int amount)>>(capacity: 25);
        // assess the needs of each station based on recipe inputs
        try
        {
            foreach (var station in stations)
            {
                if (!Core.EntityManager.Exists(station) || !station.Has<Refinementstation>() || !station.Read<NameableInteractable>().Name.ToString().ToLower().Contains("receiver")) continue;

                var refinementStation = station.Read<Refinementstation>();

                var recipesBuffer = station.ReadBuffer<RefinementstationRecipesBuffer>();
                foreach (var recipe in recipesBuffer)
                {
                    Entity recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[recipe.RecipeGuid];
                    var requirements = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
                    foreach (var requirement in requirements)
                    {
                        if (!needs.ContainsKey(requirement.Guid)) needs[requirement.Guid] = [];

                        needs[requirement.Guid].Add((station, requirement.Amount));
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogError($"Exited UpdateRefiningSystem needsProcessing early: {e}");
        }
        finally
        {
            stations.Dispose();
        }
        var providers = __instance._Query.ToEntityArray(Allocator.TempJob); // try to fulfill needs from available outputs in system query
        try
        {
            var serverGameManager = Core.ServerGameManager;
            foreach (var provider in providers)
            {
                if (!Core.EntityManager.Exists(provider) || !provider.Has<Refinementstation>() || !provider.Read<NameableInteractable>().Name.ToString().ToLower().Contains("provider")) continue;
                var refinementStation = provider.Read<Refinementstation>();

                var outputInventory = refinementStation.OutputInventoryEntity._Entity;
                foreach (var needKey in needs.Keys)
                {
                    int availableAmount = serverGameManager.GetInventoryItemCount(outputInventory, needKey);
                    if (availableAmount <= 0) continue;

                    foreach (var (station, amount) in needs[needKey])
                    {
                        if (!Utilities.SharedHeartConnection(provider, station)) continue;

                        var receivingStation = station.Read<Refinementstation>();
                        var inputInventory = receivingStation.InputInventoryEntity._Entity;

                        var transferAmount = Math.Min(amount, availableAmount);
                        Utilities.TransferItems(serverGameManager, outputInventory, inputInventory, needKey, transferAmount);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogError($"Exited UpdateRefiningSystem providerProcessing early: {e}");
        }
        finally
        {
            providers.Dispose();
        }
    }
}

[HarmonyPatch(typeof(ServantMissionUpdateSystem), nameof(ServantMissionUpdateSystem.OnUpdate))]
public static class ServantMissionUpdateSystemPatch
{
    public static void Prefix(ServantMissionUpdateSystem __instance)
    {
        var missions = __instance._TempFinishedMissions;
        if (missions.IsEmpty || !missions.IsCreated) return;
        try
        {
            foreach (var mission in missions)
            {
                if (mission.MissionOwner.Equals(Entity.Null)) continue;
                else
                {
                    var owner = mission.MissionOwner.Read<EntityOwner>().Owner;
                    var steamId = owner.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    if (!Core.PlayerSettings.GetAutoStashMissions(steamId)) continue;
                        
                    Utilities.StashServantInventory(mission.MissionOwner);
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogError($"Exited ServantMissionActionSystem hook early: {e}");
        }
    }
}