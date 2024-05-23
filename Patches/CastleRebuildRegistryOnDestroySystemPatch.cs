using HarmonyLib;
using ProjectM.CastleBuilding.Rebuilding;

namespace KindredLogistics.Patches;
[HarmonyPatch(typeof(CastleRebuildRegistryOnDestroySystem), nameof(CastleRebuildRegistryOnDestroySystem.OnUpdate))]
class CastleRebuildRegistryOnDestroySystemPatch
{
    static void Prefix(CastleRebuildRegistryOnDestroySystem __instance)
    {
        Core.TerritoryService?.FlushTerritoryCache();
    }
}
