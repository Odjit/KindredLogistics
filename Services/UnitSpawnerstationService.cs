using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.CastleBuilding;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;


namespace KindredLogistics.Services;
class UnitSpawnerstationService
{
    static readonly ComponentType[] UnitSpawnerstationQuery =
        [
            ComponentType.ReadOnly(Il2CppType.Of<Team>()),
            ComponentType.ReadOnly(Il2CppType.Of<CastleHeartConnection>()),
            ComponentType.ReadOnly(Il2CppType.Of<UnitSpawnerstation>()),
            ComponentType.ReadOnly(Il2CppType.Of<NameableInteractable>()),
            ComponentType.ReadOnly(Il2CppType.Of<UserOwner>()),
            ComponentType.ReadOnly(Il2CppType.Of<RefinementstationRecipesBuffer>()),
            ComponentType.ReadOnly(Il2CppType.Of<CastleWorkstation>()),
        ];

    EntityQuery stationsQuery;

    public UnitSpawnerstationService()
    {
        stationsQuery = Core.EntityManager.CreateEntityQuery(UnitSpawnerstationQuery);
    }

    public IEnumerable<Entity> GetAllUnitSpawners(int territoryId)
    {
        var stationArray = stationsQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (var station in stationArray)
            {
                var stationTerritoryId = Core.TerritoryService.GetTerritoryId(station);
                if (stationTerritoryId != territoryId)
                    continue;

                yield return station;
            }
        }
        finally
        {
            stationArray.Dispose();
        }
    }
}
