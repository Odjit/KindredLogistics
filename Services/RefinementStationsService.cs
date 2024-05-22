using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.CastleBuilding;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;

namespace KindredLogistics.Services
{
    internal class RefinementStationsService
    {
        static readonly ComponentType[] RefinementStationQuery =
            [
                ComponentType.ReadOnly(Il2CppType.Of<Team>()),
                ComponentType.ReadOnly(Il2CppType.Of<CastleHeartConnection>()),
                ComponentType.ReadOnly(Il2CppType.Of<Refinementstation>()),
                ComponentType.ReadOnly(Il2CppType.Of<NameableInteractable>()),
                ComponentType.ReadOnly(Il2CppType.Of<UserOwner>()),
                ComponentType.ReadOnly(Il2CppType.Of<RefinementstationRecipesBuffer>()),
                ComponentType.ReadOnly(Il2CppType.Of<CastleWorkstation>()),
            ];

        public delegate bool StationFilter(Entity station);

        EntityQuery stationsQuery;
        readonly Regex receiverRegex;
        readonly Regex senderRegex;

        public RefinementStationsService() 
        {
            stationsQuery = Core.EntityManager.CreateEntityQuery(RefinementStationQuery);
            receiverRegex = new Regex(Const.RECEIVER_REGEX, RegexOptions.Compiled);
            senderRegex = new Regex(Const.SENDER_REGEX, RegexOptions.Compiled);
        }

        public IEnumerable<Entity> GetAllStations()
        {
            var stationArray = stationsQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var station in stationArray)
                {
                    yield return station;
                }
            }
            finally
            {
                stationArray.Dispose();
            }
        }

        public IEnumerable<(int group, Entity station)> GetAllReceivingStations(int territoryId)
        {
            foreach (var result in GetAllGroupStations(receiverRegex, territoryId))
            {
                yield return result;
            }
        }

        public IEnumerable<(int group, Entity station)> GetAllSendingStations(int territoryId)
        {
            foreach(var result in GetAllGroupStations(senderRegex, territoryId))
            {
                yield return result;
            }
        }

        IEnumerable<(int group, Entity station)> GetAllGroupStations(Regex groupRegex, int territoryId)
        {
            var stationArray = stationsQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var station in stationArray)
                {
                    var stationTerritoryId = Core.TerritoryService.GetTerritoryId(station);
                    if (stationTerritoryId != territoryId)
                        continue;

                    var name = station.Read<NameableInteractable>().Name.ToString().ToLower();
                    foreach (Match match in groupRegex.Matches(name))
                    {
                        var group = int.Parse(match.Groups[1].Value);
                        yield return (group, station);
                    }
                }
            }
            finally
            {
                stationArray.Dispose();
            }
        }


    }
}
