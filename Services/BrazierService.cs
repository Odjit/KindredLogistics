using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Network;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace KindredLogistics.Services;
class BrazierService
{
    readonly Dictionary<Entity, List<Entity>> braziersByHeart = [];
    Dictionary<int, HashSet<Entity>> modifiedBraziers = [];

    public BrazierService()
    {
        var entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
            .AddAll(ComponentType.ReadOnly(Il2CppType.Of<Bonfire>()))
            .WithOptions(EntityQueryOptions.IncludeDisabled);
        var brazierQuery = Core.EntityManager.CreateEntityQuery(ref entityQueryBuilder);
        entityQueryBuilder.Dispose();

        var stationArray = brazierQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (var station in stationArray)
                AddBrazier(station);
        }
        finally
        {
            stationArray.Dispose();
        }
        brazierQuery.Dispose();

        for(var i = TerritoryService.MIN_TERRITORY_ID; i <= TerritoryService.MAX_TERRITORY_ID; i++)
        {
            modifiedBraziers.Add(i, []);
        }

        // Proximity/day-night brazier control is the one feature that cannot be inventory-event
        // driven (it depends on player position and the clock). It runs on a dedicated slow
        // ticker scoped to only the territories that actually have braziers - NOT the logistics
        // work queue and NOT the old 0..146 scan.
        Core.StartCoroutine(BrazierProximityLoop());
    }

    const float PROXIMITY_TICK_SECONDS = 2.5f;

    IEnumerator BrazierProximityLoop()
    {
        var wait = new WaitForSeconds(PROXIMITY_TICK_SECONDS);
        while (true)
        {
            yield return wait;

            if (!Core.HasInitialized) continue;
            if (!Core.PlayerSettings.IsSolarEnabled(0)) continue;

            foreach (var territoryId in GetBrazierTerritories())
            {
                var castleHeartEntity = Core.TerritoryService.GetCastleHeart(territoryId);
                if (castleHeartEntity == Entity.Null) continue;

                IEnumerator enumerator;
                try
                {
                    enumerator = UpdateIfBraziersActiveOnTerritory(territoryId, castleHeartEntity);
                }
                catch (System.Exception e)
                {
                    Core.LogException(e);
                    continue;
                }

                // UpdateIfBraziersActiveOnTerritory never yields mid-work; drain it synchronously.
                var stillRunning = true;
                while (stillRunning)
                {
                    try
                    {
                        stillRunning = enumerator.MoveNext();
                    }
                    catch (System.Exception e)
                    {
                        Core.LogException(e);
                        stillRunning = false;
                    }
                }
            }
        }
    }

    List<int> GetBrazierTerritories()
    {
        var result = new List<int>();
        foreach (var castleHeartEntity in new List<Entity>(braziersByHeart.Keys))
        {
            if (!Core.EntityManager.Exists(castleHeartEntity)) continue;
            if (!castleHeartEntity.Has<CastleHeart>()) continue;

            var territoryEntity = castleHeartEntity.Read<CastleHeart>().CastleTerritoryEntity;
            if (!Core.EntityManager.Exists(territoryEntity) || !territoryEntity.Has<CastleTerritory>()) continue;

            result.Add(territoryEntity.Read<CastleTerritory>().CastleTerritoryIndex);
        }
        return result;
    }

    internal void AddBrazier(Entity stationEntity)
    {
        var castleHeartEntity = stationEntity.Read<CastleHeartConnection>().CastleHeartEntity.GetEntityOnServer();

        if (!braziersByHeart.TryGetValue(castleHeartEntity, out var list))
        {
            list = [];
            braziersByHeart.Add(castleHeartEntity, list);
        }
        list.Add(stationEntity);
    }

    internal void RemoveBrazier(Entity stationEntity)
    {
        var castleHeartEntity = stationEntity.Read<CastleHeartConnection>().CastleHeartEntity.GetEntityOnServer();

        if (!braziersByHeart.TryGetValue(castleHeartEntity, out var list)) return;

        list.Remove(stationEntity);
    }

    public IEnumerable<Entity> GetAllBraziers(int territoryId)
    {
        var castleHeartEntity = Core.TerritoryService.GetCastleHeart(territoryId);
        if (!braziersByHeart.TryGetValue(castleHeartEntity, out var list)) yield break;

        for (var i = list.Count - 1; i >= 0; i--)
        {
            var stationEntity = list[i];
            if (!Core.EntityManager.Exists(stationEntity))
            {
                list.RemoveAt(i);
                continue;
            }
            if (stationEntity.Has<Disabled>()) continue;
            yield return stationEntity;
        }
    }

    IEnumerator UpdateIfBraziersActiveOnTerritory(int territoryId, Entity castleHeartEntity)
    {
        if (!Core.PlayerSettings.IsSolarEnabled(0)) yield break;

        // Check if any of the clan mates are online and on the territory
        var userOwner = castleHeartEntity.Read<UserOwner>();
        if (userOwner.Owner.GetEntityOnServer() == Entity.Null) yield break;

        var entitiesToCheckForProximity = new List<Entity>();
        var proxEnable = true;
        var ownerEntity = userOwner.Owner.GetEntityOnServer();
        var user = ownerEntity.Read<User>();
        var clanEntity = user.ClanEntity.GetEntityOnServer();
        if (clanEntity == Entity.Null)
        {
            var character = user.LocalCharacter.GetEntityOnServer();
            // No clan, so check only the owner
            if (!user.IsConnected || Core.TerritoryService.GetTerritoryId(character) != territoryId)
            {
                proxEnable = false;
            }
            else
            {
                entitiesToCheckForProximity.Add(character);
            }
        }
        else
        {
            var foundOnlineMemberOnTerritory = false;
            var members = Core.EntityManager.GetBuffer<ClanMemberStatus>(clanEntity);
            var userBuffer = Core.EntityManager.GetBuffer<SyncToUserBuffer>(clanEntity);
            for (var i = 0; i < members.Length; ++i)
            {
                if (!members[i].IsConnected) continue;

                var character = userBuffer[i].UserEntity.Read<User>().LocalCharacter.GetEntityOnServer();
                if (Core.TerritoryService.GetTerritoryId(character) == territoryId)
                {
                    foundOnlineMemberOnTerritory = true;
                    entitiesToCheckForProximity.Add(character);
                }
            }

            if (!foundOnlineMemberOnTerritory)
            {
                proxEnable = false;
                entitiesToCheckForProximity.Clear();
            }
        }

        var allBraziers = GetAllBraziers(territoryId);
        var modified = modifiedBraziers[territoryId];
        foreach (var brazier in allBraziers)
        {
            var nameableInteractable = brazier.Read<NameableInteractable>();
            var name = nameableInteractable.Name.ToString().ToLower();
            if (name.Contains("prox"))
            {
                const float proxDistance = 20f;

                var shouldEnable = proxEnable;
                if (shouldEnable)
                {
                    var brazierPosition = brazier.Read<Translation>().Value.xz;
                    shouldEnable = false;
                    foreach (var entity in entitiesToCheckForProximity)
                    {
                        var entityPosition = entity.Read<Translation>().Value.xz;
                        if (Vector2.Distance(brazierPosition, entityPosition) <= proxDistance)
                        {
                            shouldEnable = true;
                            break;
                        }
                    }
                }

                var burnContainer = brazier.Read<BurnContainer>();
                if (burnContainer.Enabled != shouldEnable)
                {
                    burnContainer.Enabled = shouldEnable;
                    brazier.Write(burnContainer);

                    if (!modified.Contains(brazier))
                    {
                        var bonfireTime = brazier.Read<Bonfire>();
                        bonfireTime.TimeToGetToFullStrength = 0.5f;
                        brazier.Write(bonfireTime);
                        modified.Add(brazier);
                    }
                }
            }
            else
            {
                if (modified.Contains(brazier))
                {
                    modified.Remove(brazier);
                    var bonfireTime = brazier.Read<Bonfire>();
                    bonfireTime.TimeToGetToFullStrength = 15;
                    brazier.Write(bonfireTime);
                }
            }
        }
    }
}
