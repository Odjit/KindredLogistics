using ProjectM;
using ProjectM.CastleBuilding;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

namespace KindredLogistics.Services
{
    /// <summary>
    /// Event-driven replacement for the old 0..146 territory poll. Inventory changes (and a
    /// handful of lifecycle/setting triggers) push the affected territory onto a dirty stack;
    /// the worker coroutine drains the stack, running the registered per-territory callbacks
    /// only for territories that actually changed. No periodic scan for work.
    /// </summary>
    internal class WorkQueueService
    {
        readonly HashSet<int> queued = [];
        readonly Stack<int> stack = new();

        // Territories popped while their castle was mid-rebuild. Re-enqueued when the rebuild
        // registry tears down (CastleRebuildRegistryOnDestroySystemPatch) so logistics resumes.
        readonly HashSet<int> rebuildDeferred = [];

        // The territory the worker is currently draining. Events for it during the drain set
        // reprocessCurrent instead of re-pushing, so we converge with one extra pass at most.
        int processing = -1;
        bool reprocessCurrent;

        public WorkQueueService()
        {
            Core.StartCoroutine(Drain());
        }

        public int QueueDepth => stack.Count;

        public bool IsQueued(int territoryId) => territoryId == processing || queued.Contains(territoryId);

        public void Enqueue(int territoryId)
        {
            if (territoryId < TerritoryService.MIN_TERRITORY_ID || territoryId > TerritoryService.MAX_TERRITORY_ID)
                return;

            if (territoryId == processing)
            {
                reprocessCurrent = true;
                return;
            }

            if (!queued.Add(territoryId)) return;
            stack.Push(territoryId);
        }

        public void EnqueueOwner(Entity owner)
        {
            if (owner == Entity.Null) return;
            Enqueue(Core.TerritoryService.GetTerritoryId(owner));
        }

        public void EnqueueAll()
        {
            for (var i = TerritoryService.MIN_TERRITORY_ID; i <= TerritoryService.MAX_TERRITORY_ID; i++)
            {
                if (Core.TerritoryService.GetCastleHeart(i) != Entity.Null)
                    Enqueue(i);
            }
        }

        internal void FlushRebuildDeferred()
        {
            if (rebuildDeferred.Count == 0) return;

            var toRequeue = new List<int>(rebuildDeferred);
            rebuildDeferred.Clear();
            foreach (var territoryId in toRequeue)
                Enqueue(territoryId);
        }

        IEnumerator Drain()
        {
            yield return null;
            Core.TerritoryService.StartTimer();
            while (true)
            {
                if (stack.Count == 0)
                {
                    yield return null;
                    Core.TerritoryService.StartTimer();
                    continue;
                }

                if (Core.TerritoryService.ShouldUpdateYield())
                {
                    yield return null;
                    Core.TerritoryService.StartTimer();
                }

                var territoryId = stack.Pop();
                queued.Remove(territoryId);

                var castleHeartEntity = Core.TerritoryService.GetCastleHeart(territoryId);
                if (castleHeartEntity == Entity.Null) continue;

                // Defer rather than drop while a castle is rebuilding so the work isn't lost.
                if (Core.TerritoryService.IsTerritoryRebuilding(territoryId) ||
                    castleHeartEntity.Read<CastleRebuildPhaseState>().State != PhaseState.None)
                {
                    rebuildDeferred.Add(territoryId);
                    continue;
                }

                processing = territoryId;
                reprocessCurrent = false;

                foreach (var callback in Core.TerritoryService.UpdateCallbacks)
                {
                    IEnumerator enumerator = null;
                    bool stillRunning = false;
                    try
                    {
                        enumerator = callback(territoryId, castleHeartEntity);
                        stillRunning = enumerator.MoveNext();
                    }
                    catch (Exception e)
                    {
                        Core.LogException(e);
                    }

                    while (stillRunning)
                    {
                        yield return null;
                        Core.TerritoryService.StartTimer();

                        try
                        {
                            stillRunning = enumerator.MoveNext();
                        }
                        catch (Exception e)
                        {
                            Core.LogException(e);
                        }
                    }

                    if (Core.TerritoryService.ShouldUpdateYield())
                    {
                        yield return null;
                        Core.TerritoryService.StartTimer();
                    }
                }

                processing = -1;
                if (reprocessCurrent)
                    Enqueue(territoryId);
            }
        }
    }
}
