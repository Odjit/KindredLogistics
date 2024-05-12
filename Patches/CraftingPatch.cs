using HarmonyLib;
using KindredLogistics.Services;
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
                        
                        var workstation = new Entity();
                        workstation.Index = stopCraftEvent.Workstation.Normal_Index;
                        workstation.Version = stopCraftEvent.Workstation.Normal_Generation;
                        PullService.HandleRecipePull(fromCharacter.Character, workstation, stopCraftEvent.RecipeGuid);
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