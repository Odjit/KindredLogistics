using HarmonyLib;
using ProjectM;
using ProjectM.CastleBuilding;
using Unity.Collections;

namespace KindredLogistics.Patches;

[HarmonyPatch(typeof(CastleHasItemsOnSpawnSystem), nameof(CastleHasItemsOnSpawnSystem.OnUpdate))]
internal class CastleStationSpawnSystemPatch
{
    public static bool Prefix(CastleHasItemsOnSpawnSystem __instance)
    {
        var entities = __instance.__query_60442477_0.ToEntityArray(Allocator.Temp);
        foreach (var castleConnectionEntity in entities)
        {
            if (castleConnectionEntity.Has<Bonfire>()) Core.BrazierService.AddBrazier(castleConnectionEntity);
            if (castleConnectionEntity.Has<Refinementstation>()) Core.RefinementStations.AddRefinementStation(castleConnectionEntity);
            if (castleConnectionEntity.Has<Salvagestation>()) Core.SalvageService.AddSalvageStation(castleConnectionEntity);
            if (castleConnectionEntity.Has<UnitSpawnerstation>()) Core.UnitSpawnerstationService.AddUnitSpawnerStation(castleConnectionEntity);

            // A newly built/rebuilt station means its territory has work to (re)evaluate.
            Core.WorkQueue?.EnqueueOwner(castleConnectionEntity);
        }
        entities.Dispose();
        return true;
    }
}

[HarmonyPatch(typeof(CastleHasItemsOnDestroySystem), nameof(CastleHasItemsOnDestroySystem.OnUpdate))]
internal class CastleStationDestroySystemPatch
{
    public static bool Prefix(CastleHasItemsOnDestroySystem __instance)
    {
        var entities = __instance._DestroyConnectedCastleItem.ToEntityArray(Allocator.Temp);
        foreach (var castleConnectionEntity in entities)
        {
            if (castleConnectionEntity.Has<Bonfire>()) Core.BrazierService.RemoveBrazier(castleConnectionEntity);
            if (castleConnectionEntity.Has<Refinementstation>()) Core.RefinementStations.RemoveRefinementStation(castleConnectionEntity);
            if (castleConnectionEntity.Has<Salvagestation>()) Core.SalvageService.RemoveSalvageStation(castleConnectionEntity);
            if (castleConnectionEntity.Has<UnitSpawnerstation>()) Core.UnitSpawnerstationService.RemoveUnitSpawnerStation(castleConnectionEntity);
        }
        entities.Dispose();
        return true;
    }
}