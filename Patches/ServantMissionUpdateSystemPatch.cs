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
    static void Prefix(ServantMissionUpdateSystem __instance)
    {
        var missions = __instance._TempFinishedMissions;
        var servants = __instance._TempServantList;
        try
        {
            foreach (var mission in missions)
            {
                var owner = mission.MissionOwner.Read<UserOwner>().Owner._Entity;
                var steamId = owner.Read<User>().PlatformId;
                if (!Core.PlayerSettings.IsAutoStashMissionsEnabled(steamId)) continue;
                foreach (var servant in servants)
                {
                    Utilities.StashServantInventory(servant);
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogError($"Exited ServantMissionActionSystem hook early: {e}");
        }
    }
}
