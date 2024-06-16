using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Terrain;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace KindredLogistics.Services
{
    internal class TerritoryService
    {
        Dictionary<WorldRegionType, List<Entity>> territories = [];
        Dictionary<Entity, int> territoryCache = [];

        public TerritoryService()
        {
            // Load Territories
            EntityQueryDesc queryDesc = new()
            {
                All = new ComponentType[] { new(Il2CppType.Of<CastleTerritory>(), ComponentType.AccessMode.ReadWrite) },
                Options = EntityQueryOptions.Default
            };

            var query = Core.EntityManager.CreateEntityQuery(queryDesc);

            foreach (var territoryEntity in query.ToEntityArray(Allocator.Temp))
            {
                var region = territoryEntity.Read<TerritoryWorldRegion>().Region;

                if (!territories.TryGetValue(region, out var territoriesInRegion))
                {
                    territoriesInRegion = [];
                    territories[region] = territoriesInRegion;
                }
                territoriesInRegion.Add(territoryEntity);
            }
        }

        public void FlushTerritoryCache()
        {
            territoryCache.Clear();
        }

        public int GetTerritoryId(Entity entity)
        {
            if (territoryCache.TryGetValue(entity, out var territoryId))
            {
                return territoryId;
            }

            if (entity.Has<CastleHeartConnection>())
            {
                var heart = entity.Read<CastleHeartConnection>().CastleHeartEntity.GetEntityOnServer();

                if (heart != Entity.Null)
                {
                    var castleHeart = heart.Read<CastleHeart>();
                    var castleTerritory = castleHeart.CastleTerritoryEntity;

                    // Cache the territory id of buildings as they don't change
                    territoryId = castleTerritory.Read<CastleTerritory>().CastleTerritoryIndex;
                    territoryCache[entity] = territoryId;
                    return territoryId;
                }
            }

            if (entity.Has<TilePosition>())
            {
                var region = Core.RegionService.GetRegion(entity);
                var tilePos = entity.Read<TilePosition>();
                if (territories.TryGetValue(region, out var territoriesInRegion))
                {
                    for (int i = 0; i < territoriesInRegion.Count; i++)
                    {
                        var territory = territoriesInRegion[i];
                        if (CastleTerritoryExtensions.IsTileInTerritory(Core.EntityManager, tilePos.Tile, ref territory, out var _))
                        {
                            return territory.Read<CastleTerritory>().CastleTerritoryIndex;
                        }
                    }
                }
            }
            return -1;
        }
    }
}
