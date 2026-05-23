using HarmonyLib;
using ProjectM.CastleBuilding.Rebuilding;

namespace KindredLogistics.Patches;
[HarmonyPatch(typeof(CastleRebuildRegistryOnDestroySystem), nameof(CastleRebuildRegistryOnDestroySystem.OnUpdate))]
class CastleRebuildRegistryOnDestroySystemPatch
{
    static void Prefix(CastleRebuildRegistryOnDestroySystem __instance)
    {
        Core.TerritoryService?.FlushTerritoryCache();

        // A rebuild has settled (registry teardown). Re-enqueue any territory the worker had to
        // defer while it was mid-rebuild so logistics resumes there.
        Core.WorkQueue?.FlushRebuildDeferred();
    }
}
