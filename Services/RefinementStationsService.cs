using ProjectM.CastleBuilding;
using ProjectM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Il2CppInterop.Runtime;
using System.Collections;
using Unity.Collections;
using System.Text.RegularExpressions;

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

        public IEnumerable<(int territoryIndex, int group, Entity station)> GetAllReceivingStations(StationFilter filter = null)
        {
            foreach (var result in GetAllGroupStations(receiverRegex, filter))
            {
                yield return result;
            }
        }

        public IEnumerable<(int territoryIndex, int group, Entity station)> GetAllSendingStations(StationFilter filter = null)
        {
            foreach(var result in GetAllGroupStations(senderRegex, filter))
            {
                yield return result;
            }
        }

        IEnumerable<(int territoryIndex, int group, Entity station)> GetAllGroupStations(Regex groupRegex, StationFilter filter = null)
        {
            var stationArray = stationsQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var station in stationArray)
                {
                    if (filter != null && !filter(station))
                        continue;

                    var name = station.Read<NameableInteractable>().Name.ToString().ToLower();
                    foreach (Match match in groupRegex.Matches(name))
                    {
                        var group = int.Parse(match.Groups[1].Value);
                        yield return (Core.TerritoryService.GetTerritoryId(station), group, station);
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
