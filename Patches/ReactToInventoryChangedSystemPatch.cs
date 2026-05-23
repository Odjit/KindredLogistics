using HarmonyLib;
using ProjectM;
using ProjectM.CastleBuilding;
using Unity.Collections;
using Unity.Entities;

namespace KindredLogistics.Patches;

/// <summary>
/// Consumer-side hook on the universal inventory-change event. Every authoritative server-side
/// inventory mutation routes through InventoryUtilitiesServer.CreateInventoryChangedEvent, whose
/// events are read here last (all built-in consumers have already run). We only need to learn
/// WHICH inventory changed so we can enqueue its territory for processing - no content tracking.
/// Pattern adapted from Bloodcraft (mfoltz).
/// </summary>
[HarmonyPatch(typeof(ReactToInventoryChangedSystem), nameof(ReactToInventoryChangedSystem.OnUpdate))]
internal static class ReactToInventoryChangedSystemPatch
{
    [HarmonyPrefix]
    static void Prefix(ReactToInventoryChangedSystem __instance)
    {
        if (!Core.HasInitialized || Core.WorkQueue == null) return;

        var inventoryChangedEvents = __instance.EntityQueries[0].ToComponentDataArray<InventoryChangedEvent>(Allocator.Temp);
        try
        {
            foreach (var inventoryChangedEvent in inventoryChangedEvents)
            {
                var inventory = inventoryChangedEvent.InventoryEntity;

                // Resolve the inventory entity to its owning station/stash. Player, servant and
                // dropped-item inventories have no InventoryConnection/CastleHeartConnection, so
                // this cheaply filters out all the noise we don't care about.
                if (!inventory.Has<InventoryConnection>()) continue;
                var owner = inventory.Read<InventoryConnection>().InventoryOwner;
                if (owner == Entity.Null || !owner.Has<CastleHeartConnection>()) continue;

                Core.WorkQueue.EnqueueOwner(owner);
            }
        }
        catch (System.Exception e)
        {
            Core.LogException(e);
        }
        finally
        {
            inventoryChangedEvents.Dispose();
        }
    }
}
