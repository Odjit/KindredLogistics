using HarmonyLib;
using KindredLogistics;
using ProjectM;
using ProjectM.Network;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace KindreddLogistics.Patches
{
    [HarmonyPatch]
    public class SortSingleInventorySystemPatch
    {
        // Would be better as a circular buffer but in general this will be one element so doesn't really matter
        static List<(ulong, double)> lastSort = [];

        [HarmonyPatch(typeof(SortSingleInventorySystem), nameof(SortSingleInventorySystem.OnUpdate))]
        [HarmonyPrefix]
        static void Prefix(SortSingleInventorySystem __instance)
        {
            var entities = __instance._EventQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Equals(Entity.Null)) continue;

                    var fromCharacter = entity.Read<FromCharacter>();
                    var steamId = fromCharacter.User.Read<User>().PlatformId;

                    if (!Core.PlayerSettings.IsSortStashEnabled(steamId)) continue;

                    var serverTime = Core.ServerTime;
                    var found = false;
                    for(int i = 0; i < lastSort.Count; i++)
                    {
                        if (lastSort[i].Item1 == steamId)
                        {
                            var lastSortTime = lastSort[i].Item2;
                            if ((serverTime - lastSortTime) < 1)
                            {
                                found = true;
                                Core.Stash.StashCharacterInventory(fromCharacter.Character);
                            }

                            lastSort.RemoveAt(i);
                            break;
                        }
                    }

                    if(!found)
                    {
                        lastSort.Add((steamId, serverTime));
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Log.LogError(ex);
            }
            finally
            {
                entities.Dispose();
            }
        }
        
    }
}