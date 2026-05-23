using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;

namespace KindredLogistics.Patches;

/// <summary>
/// Enabling/disabling a recipe at a refinement (or unit spawner) station changes what logistics
/// should feed it, but flips RefinementstationRecipesBuffer state without firing an inventory
/// event. The client sends a ToggleRefiningRecipeEvent carrying the station's NetworkId; resolve
/// it and enqueue that station's territory.
///
/// Must be a PREFIX: ToggleRefiningRecipeSystem.OnUpdate consumes the event entities, so a postfix
/// sees an empty query (confirmed via diagnostic logging 2026-05-22).
/// </summary>
[HarmonyPatch(typeof(ToggleRefiningRecipeSystem), nameof(ToggleRefiningRecipeSystem.OnUpdate))]
internal static class ToggleRefiningRecipeSystemPatch
{
    [HarmonyPrefix]
    static void Prefix(ToggleRefiningRecipeSystem __instance)
    {
        if (!Core.HasInitialized || Core.WorkQueue == null) return;

        var toggleEvents = __instance._EventQuery.ToComponentDataArray<ToggleRefiningRecipeEvent>(Allocator.Temp);
        try
        {
            foreach (var toggleEvent in toggleEvents)
            {
                if (Core.TryGetEntityFromNetworkId(toggleEvent.RefinementStation, out var station))
                    Core.WorkQueue.EnqueueOwner(station);
            }
        }
        catch (System.Exception e)
        {
            Core.LogException(e);
        }
        finally
        {
            toggleEvents.Dispose();
        }
    }
}
