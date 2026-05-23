using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;

namespace KindredLogistics.Patches;

/// <summary>
/// Renaming a stash/chest changes its logistics role (sender/receiver/overflow/salvage/spawner/
/// brazier) but fires no inventory event, so the event-driven work queue would never re-evaluate
/// it. Resolve the renamed interactable from the event's NetworkId and enqueue its territory.
///
/// Must be a PREFIX: NameableInteractableSystem.OnUpdate consumes the RenameInteractable event
/// entities, so a postfix sees an empty query (confirmed via diagnostic logging 2026-05-22).
/// </summary>
[HarmonyPatch(typeof(NameableInteractableSystem), nameof(NameableInteractableSystem.OnUpdate))]
internal static class NameableInteractableSystemPatch
{
    [HarmonyPrefix]
    static void Prefix(NameableInteractableSystem __instance)
    {
        if (!Core.HasInitialized || Core.WorkQueue == null) return;

        var renameEvents = __instance._RenameQuery.ToComponentDataArray<InteractEvents_Client.RenameInteractable>(Allocator.Temp);
        try
        {
            foreach (var renameEvent in renameEvents)
            {
                if (Core.TryGetEntityFromNetworkId(renameEvent.InteractableId, out var interactable))
                    Core.WorkQueue.EnqueueOwner(interactable);
            }
        }
        catch (System.Exception e)
        {
            Core.LogException(e);
        }
        finally
        {
            renameEvents.Dispose();
        }
    }
}
