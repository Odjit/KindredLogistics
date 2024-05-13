using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using System;
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
                    if (!Core.PlayerSettings.IsAutoStashMissionsEnabled(steamId)) continue;
                        
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