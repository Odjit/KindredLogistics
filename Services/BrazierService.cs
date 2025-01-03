using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;

namespace KindredLogistics.Services;
class BrazierService
{
    EntityQuery brazierQuery;

    public BrazierService()
    {
        brazierQuery = Core.EntityManager.CreateEntityQuery(
                                ComponentType.ReadOnly(Il2CppType.Of<Bonfire>())
                              );

        Core.TerritoryService.RegisterTerritoryUpdateCallback(UpdateIfBraziersActiveOnTerritory);
    }

    public IEnumerable<Entity> GetAllBraziers(int territoryId)
    {
        var brazierArray = brazierQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (var brazier in brazierArray)
            {
                if (Core.TerritoryService.GetTerritoryId(brazier) != territoryId) continue;
                yield return brazier;
            }
        }
        finally
        {
            brazierArray.Dispose();
        }
    }

    void UpdateIfBraziersActiveOnTerritory(int territoryId, Entity castleHeartEntity)
    {
        if (!Core.PlayerSettings.IsSolarEnabled(0)) return;

        var enable = Core.ServerGameManager.DayNightCycle.TimeOfDay == TimeOfDay.Day;
        foreach (var brazier in GetAllBraziers(territoryId))
        {
            var nameableInteractable = brazier.Read<NameableInteractable>();
            if (!nameableInteractable.Name.ToString().ToLower().Contains("solar")) continue;

            var burnContainer = brazier.Read<BurnContainer>();
            burnContainer.Enabled = enable;
            brazier.Write(burnContainer);
        }
    }
}
