using HarmonyLib;
using KindredLogistics.Services;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using static ProjectM.Metrics;

namespace KindredLogistics.Patches;

public class CraftingPatch
{
    private static Dictionary<NetworkId, HashSet<PrefabGUID>> StationCraftingJobs = [];

    [HarmonyPatch(typeof(StartCraftingSystem), nameof(StartCraftingSystem.OnUpdate))]
    public static class StartCraftingSystemPatch
    {
        public static void Prefix(StartCraftingSystem __instance)
        {
            NativeArray<Entity> entities = __instance._StartCraftItemEventQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Has<StartCraftItemEvent>() && entity.Has<FromCharacter>())
                    {
                        FromCharacter fromCharacter = entity.Read<FromCharacter>();
                        ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                        StartCraftItemEvent startCraftItemEvent = entity.Read<StartCraftItemEvent>();
                        NetworkId networkId = startCraftItemEvent.Workstation;
                        PrefabGUID prefabGUID = startCraftItemEvent.RecipeId;
                        if (StationCraftingJobs.TryGetValue(networkId, out var craftingJobs))
                        {
                            craftingJobs.Add(prefabGUID);
                        }
                        else
                        {
                            StationCraftingJobs.Add(networkId, [prefabGUID]);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Exited StartCraftingSystem hook early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }
    }

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
                        Entity station = fromCharacter.Character.Read<Interactor>().Target; // station entity
                        NetworkId networkId = stopCraftEvent.Workstation;
                        ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                        PrefabGUID prefabGUID = stopCraftEvent.RecipeGuid;
                        if (!Core.PlayerSettings.IsCraftPullEnabled(steamId)) continue;
                        if (StationCraftingJobs.TryGetValue(networkId, out var craftingJobs) && craftingJobs.Contains(prefabGUID))
                        {
                            craftingJobs.Remove(prefabGUID);
                            continue;
                        }
                        else
                        {
                            PullService.HandleRecipePull(fromCharacter.Character, station, prefabGUID);
                        }
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
    }

    
}