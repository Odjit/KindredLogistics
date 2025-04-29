using HarmonyLib;
using ProjectM;
using Unity.Collections;
namespace KindredLogistics.Patches;

[HarmonyPatch(typeof(BonfireSystem_Server), "OnUpdate")]
public static class BonfireSystem_ServerPatch
{
    static bool wasNight;
    public static void Prefix(BonfireSystem_Server __instance)
    {
        if (!Core.PlayerSettings.IsSolarEnabled(0)) return;
        if (Core.ServerGameManager.DayNightCycle.TimeOfDay == TimeOfDay.Night)
        {
            wasNight = true;
            return;
        }

        if (!wasNight) return;
        wasNight = false;

        var entities = __instance.__query_1818188685_0.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities)
        {
            var burnContainer = entity.Read<BurnContainer>();
            var nameableInteractable = entity.Read<NameableInteractable>();
            var name = nameableInteractable.Name.ToString().ToLower();
            if (burnContainer.Enabled && name.Contains("night"))
            {
                var bonfire = entity.Read<Bonfire>();
                bonfire.IsActive = false;
                entity.Write(bonfire);
            }
        }
        entities.Dispose();
    }

    public static void Postfix(BonfireSystem_Server __instance)
    {
        if (!Core.PlayerSettings.IsSolarEnabled(0)) return;
        if (Core.ServerGameManager.DayNightCycle.TimeOfDay == TimeOfDay.Day) return;

        var entities = __instance.__query_1818188685_0.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities)
        {
            var burnContainer = entity.Read<BurnContainer>();
            var nameableInteractable = entity.Read<NameableInteractable>();
            var name = nameableInteractable.Name.ToString().ToLower();
            if (burnContainer.Enabled && name.Contains("night"))
            {
                var bonfire = entity.Read<Bonfire>();
                bonfire.IsActive = true;
                entity.Write(bonfire);
            }
        }
        entities.Dispose();
    }
}