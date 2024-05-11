using Il2CppInterop.Runtime;
using ProjectM.Terrain;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace KindredLogistics.Services
{
    internal class RegionService
    {
        struct RegionPolygon
        {
            public WorldRegionType Region;
            public Aabb Aabb;
            public float2[] Vertices;
        };

        List<RegionPolygon> regionPolygons = new();

        public RegionService()
        {
            EntityQueryDesc queryDesc = new()
            {
                All = new ComponentType[] { new(Il2CppType.Of<WorldRegionPolygon>(), ComponentType.AccessMode.ReadWrite) },
                Options = EntityQueryOptions.Default
            };

            var query = Core.EntityManager.CreateEntityQuery(queryDesc);
            foreach (var worldRegionPolygonEntity in query.ToEntityArray(Allocator.Temp))
            {
                var wrp = worldRegionPolygonEntity.Read<WorldRegionPolygon>();
                var vertices = Core.EntityManager.GetBuffer<WorldRegionPolygonVertex>(worldRegionPolygonEntity);

                regionPolygons.Add(
                    new RegionPolygon
                    {
                        Region = wrp.WorldRegion,
                        Aabb = wrp.PolygonBounds,
                        Vertices = vertices.ToNativeArray(allocator: Allocator.Temp).ToArray().Select(x => x.VertexPos).ToArray()
                    });
            }
            query.Dispose();
        }

        public WorldRegionType GetRegion(Entity entity)
        {
            return GetRegion(entity.Read<Translation>().Value);
        }

        public WorldRegionType GetRegion(float3 pos)
        {
            foreach (var worldRegionPolygon in regionPolygons)
            {
                if (worldRegionPolygon.Aabb.Contains(pos))
                {
                    if (IsPointInPolygon(worldRegionPolygon.Vertices, pos.xz))
                    {
                        return worldRegionPolygon.Region;
                    }
                }
            }
            return WorldRegionType.None;
        }

        static bool IsPointInPolygon(float2[] polygon, Vector2 point)
        {
            int intersections = 0;
            int vertexCount = polygon.Length;

            for (int i = 0, j = vertexCount - 1; i < vertexCount; j = i++)
            {
                if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                    (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
                {
                    intersections++;
                }
            }

            return intersections % 2 != 0;
        }
    }
}
