using HarmonyLib;
using KindredLogistics.Services;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
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
                        var fromCharacter = entity.Read<FromCharacter>();
                        ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                        if (!Core.PlayerSettings.IsCraftPullEnabled(steamId)) continue;

                        var stopCraftEvent = entity.Read<StopCraftItemEvent>();
                        Entity station = fromCharacter.Character.Read<Interactor>().Target; // station entity

                        if (!station.Has<QueuedWorkstationCraftAction>()) continue;

                        PrefabGUID prefabGUID = stopCraftEvent.RecipeGuid;

                        var alreadyCraftingRecipe = false;
                        var queuedActions = Core.EntityManager.GetBuffer<QueuedWorkstationCraftAction>(station);
                        foreach (var action in queuedActions)
                        {
                            if (action.RecipeGuid.Equals(prefabGUID))
                            {
                                alreadyCraftingRecipe = true;
                                break;
                            }
                        }
                        if (alreadyCraftingRecipe)
                            continue;
                        
                        PullService.HandleRecipePull(fromCharacter.Character, station, prefabGUID);
                    }
                }
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
                    ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                    if (!Core.PlayerSettings.IsCraftPullEnabled(steamId)) continue;

                    Entity station = fromCharacter.Character.Read<Interactor>().Target; // station entity

                    if (!station.Has<Forge_Shared>()) continue;

                    Forge_Shared forge_Shared = station.Read<Forge_Shared>();
                    Entity itemEntity = forge_Shared.ItemEntity._Entity;

                    if (forge_Shared.State.Equals(ForgeState.Repairing)) continue;
                    if (itemEntity.Has<ShatteredItem>())
                    {
                        PullService.HandleForgePull(fromCharacter.Character, station, itemEntity);
                    }
                    else if (itemEntity.Has<UpgradeableLegendaryItem>())
                    {
                        PullService.HandleForgeUpgradePull(fromCharacter.Character, station, itemEntity);
                    }
                }
            }
            finally
            {
                entities.Dispose();
            }
        }
    }
}