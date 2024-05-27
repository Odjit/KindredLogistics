using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared.Systems;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace KindredLogistics.Patches;

[HarmonyPatch(typeof(ServantMissionUpdateSystem), nameof(ServantMissionUpdateSystem.OnUpdate))]
public static class ServantMissionUpdateSystemPatch
{
    static readonly Dictionary<Entity, bool> wasDisabled = [];
    static void Prefix(ServantMissionUpdateSystem __instance)
    {
        var missions = __instance._TempFinishedMissions;
        var servants = __instance._TempServantList;
        List<Entity> missionServants = [];
        try
        {
            foreach (var mission in missions)
            {
                var owner = mission.MissionOwner.Read<UserOwner>().Owner._Entity;
                var steamId = owner.Read<User>().PlatformId;
                if (!Core.PlayerSettings.IsAutoStashMissionsEnabled(steamId)) continue;
                
                foreach (var servant in missionServants)
                {
                    Utilities.StashServantInventory(servant);
                    RestoreDisabled(servant);
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogError($"Exited ServantMissionActionSystem hook early: {e}");
        }
    }
    static void HandleDisabled(Entity entity, bool disabled)
    {
        entity.Remove<DisableWhenNoPlayersInRange>();
        entity.Remove<DisabledDueToNoPlayersInRange>();
        wasDisabled[entity] = disabled;
    }
    static void RestoreDisabled(Entity entity)
    {
        if (wasDisabled.TryGetValue(entity, out var disabled) && disabled)
        {
            entity.Add<DisableWhenNoPlayersInRange>();
            entity.Add<DisabledDueToNoPlayersInRange>();
            wasDisabled.Remove(entity);
        }
    }
}
