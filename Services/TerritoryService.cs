using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Terrain;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace KindredLogistics.Services
{
    internal class TerritoryService
    {
        readonly Dictionary<WorldRegionType, List<Entity>> territories = [];
        readonly Dictionary<Entity, int> territoryCache = [];

        readonly List<Action<int, Entity>> territoryUpdateCallbacks = [];

        public const int MIN_TERRITORY_ID = 0;
        public const int MAX_TERRITORY_ID = 146;

        EntityQuery castleHeartQuery;
        readonly Dictionary<int, Entity> territoryToCastleHeart = [];

        public TerritoryService()
        {
            // Load Territories
            var entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
            entityQueryBuilder.AddAll(new(Il2CppType.Of<CastleTerritory>(), ComponentType.AccessMode.ReadWrite));

            var query = Core.EntityManager.CreateEntityQuery(ref entityQueryBuilder);
            entityQueryBuilder.Dispose();

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

            entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
                .AddAll(new(Il2CppType.Of<CastleHeart>(), ComponentType.AccessMode.ReadOnly));

            castleHeartQuery = Core.EntityManager.CreateEntityQuery(ref entityQueryBuilder);
            entityQueryBuilder.Dispose();

            Core.StartCoroutine(UpdateLoop());
        }

        public void RegisterTerritoryUpdateCallback(Action<int, Entity> callback)
        {
            territoryUpdateCallbacks.Add(callback);
        }

        IEnumerator UpdateLoop()
        {
            yield return null;
            while (true)
            {
                var castleHeartEntities = castleHeartQuery.ToEntityArray(Allocator.Temp);
                try
                {
                    foreach (var castleHeartEntity in castleHeartEntities)
                    {
                        var castleHeart = castleHeartEntity.Read<CastleHeart>();
                        var territoryEntity = castleHeart.CastleTerritoryEntity;
                        var territory = territoryEntity.Read<CastleTerritory>();
                        territoryToCastleHeart[territory.CastleTerritoryIndex] = castleHeartEntity;
                    }
                }
                finally
                {
                    castleHeartEntities.Dispose();
                }

                for (int i = MIN_TERRITORY_ID; i <= MAX_TERRITORY_ID; i++)
                {
                    yield return null;

                    if (!territoryToCastleHeart.TryGetValue(i, out var castleHeartEntity)) continue;

                    // This was cached a while ago so it could be invalid now
                    if (!Core.EntityManager.Exists(castleHeartEntity))
                    {
                        territoryToCastleHeart.Remove(i);
                        continue;
                    }

                    foreach (var callback in territoryUpdateCallbacks)
                    {
                        try
                        {
                            callback(i, castleHeartEntity);
                        }
                        catch (Exception e)
                        {
                            Core.LogException(e);
                        }
                    }
                }
            }
        }

        public Entity GetCastleHeart(int territoryId)
        {
            if (!territoryToCastleHeart.TryGetValue(territoryId, out var castleHeartEntity))
                return Entity.Null;

            if (!Core.EntityManager.Exists(castleHeartEntity))
                return Entity.Null;

            return castleHeartEntity;
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

                if (Core.EntityManager.Exists(heart) && heart != Entity.Null)
                {
                    var castleHeart = heart.Read<CastleHeart>();
                    var castleTerritory = castleHeart.CastleTerritoryEntity;

                    // Cache the territory id of buildings as they don't change
                    if (castleTerritory.Has<CastleTerritory>())
                    {
                        territoryId = castleTerritory.Read<CastleTerritory>().CastleTerritoryIndex;
                        territoryCache[entity] = territoryId;
                        return territoryId;
                    }
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
                            if (territory.Has<CastleTerritory>()) return territory.Read<CastleTerritory>().CastleTerritoryIndex;
                        }
                    }
                }
            }
            return -1;
        }
    }
}
