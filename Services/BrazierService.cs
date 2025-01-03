using Il2CppInterop.Runtime;
using ProjectM;
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
}
