using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using System;
using System.Collections.Generic;
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
}

[HarmonyPatch(typeof(ServantMissionUpdateSystem), nameof(ServantMissionUpdateSystem.OnUpdate))]
public static class ServantMissionUpdateSystemPatch
{
    public static void Prefix(ServantMissionUpdateSystem __instance)
    {
        var missions = __instance._TempFinishedMissions;
        var servants = __instance._TempServantList;
        try
        {
            foreach (var mission in missions)
            {
                if (mission.MissionOwner.Equals(Entity.Null)) continue;
                else
                {
                    List<Entity> missionServants = [];
                    foreach (Entity entity in servants)
                    {
                        if (entity.Equals(Entity.Null)) continue;
                        missionServants.Add(entity);
                    }
                    var owner = mission.MissionOwner.Read<UserOwner>().Owner._Entity;
                    var steamId = owner.Read<User>().PlatformId;
                    if (!Core.PlayerSettings.IsAutoStashMissionsEnabled(steamId)) continue;
                    foreach (var servant in missionServants)
                    {
                        Utilities.StashServantInventory(servant);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogError($"Exited ServantMissionActionSystem hook early: {e}");
        }
    }
}