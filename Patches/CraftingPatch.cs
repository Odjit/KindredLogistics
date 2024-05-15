using HarmonyLib;
using KindredLogistics.Services;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
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
                        Entity station = fromCharacter.Character.Read<Interactor>().Target; // station entity
                        NetworkId networkId = stopCraftEvent.Workstation;
                        ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                        PrefabGUID prefabGUID = stopCraftEvent.RecipeGuid;
                        if (!Core.PlayerSettings.IsCraftPullEnabled(steamId)) continue;

                        var alreadyCraftingRecipe = false;
                        var queuedActions = Core.EntityManager.GetBuffer<QueuedWorkstationCraftAction>(station);
                        if (queuedActions.Length > 0)
                        {
                            foreach (var action in queuedActions)
                            {
                                if (action.RecipeGuid.Equals(prefabGUID))
                                {
                                    alreadyCraftingRecipe = true;
                                    break;
                                }
                            }
                        }
                        if (alreadyCraftingRecipe)
                            continue;
                        
                        PullService.HandleRecipePull(fromCharacter.Character, station, prefabGUID);
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
    [HarmonyPatch(typeof(ForgeSystem_Events), nameof(ForgeSystem_Events.OnUpdate))]
    public static class ForgeSystem_EventsPatch
    {
        public static void Prefix(ForgeSystem_Events __instance)
        {
            var entities = __instance._CancelRepairEventQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    var fromCharacter = entity.Read<FromCharacter>();
                    Entity station = fromCharacter.Character.Read<Interactor>().Target; // station entity
                    Forge_Shared forge_Shared = station.Read<Forge_Shared>();
                    Entity itemEntity = forge_Shared.ItemEntity._Entity;
                    PrefabGUID prefabGUID = itemEntity.Read<PrefabGUID>();
                    ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                    if (!Core.PlayerSettings.IsCraftPullEnabled(steamId)) continue;
                    if (forge_Shared.State.Equals(ForgeState.Repairing)) continue;
                    if (itemEntity.Has<ShatteredItem>())
                    {
                        PullService.HandleRecipePull(fromCharacter.Character, station, prefabGUID);
                        return;
                    }
                    if (itemEntity.Has<UpgradeableLegendaryItem>())
                    {
                        PullService.HandleUpgrade(fromCharacter.Character, station, itemEntity);
                    }
                    

                }
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Exited ForgeSystem_Events hook early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }
    }
    
    





}