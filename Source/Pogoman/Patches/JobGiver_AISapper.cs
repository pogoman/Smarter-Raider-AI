using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using Verse.AI.Group;
using Unity.Baselib.LowLevel;
using UnityEngine;
using System.Net.NetworkInformation;
using Unity.Jobs;
using Verse.Noise;

namespace PogoAI.Patches
{
    internal class JobGiver_AISapper
    {
        const int cacheExpiryTime = 20; //Seconds

        /// <summary>
        /// distance to reuse pathcost calc
        /// </summary>
        const int cacheCellDistanceMax = 10;
        
        /// <summary>
        /// pawn pos, target pos, blocking thing, cell before, cache time
        /// </summary>
        static List<Tuple<IntVec3, IntVec3, Thing, IntVec3, float>> pathCostCache = new List<Tuple<IntVec3, IntVec3, Thing, IntVec3, float>>();

        static Dictionary<string, Thing> lastAttackTarget = new Dictionary<string, Thing>();

        [HarmonyPatch(typeof(RimWorld.JobGiver_AISapper), "TryGiveJob")]
        static class JobGiver_AISapper_TryGiveJob_Patch
        {
            static bool Prefix(Pawn pawn, ref Job __result, RimWorld.JobGiver_AISapper __instance)
            {
                IntVec3 intVec = pawn.mindState.duty.focus.Cell;
                if (intVec.IsValid && (float)intVec.DistanceToSquared(pawn.Position) < 100f && intVec.GetRoom(pawn.Map) == pawn.GetRoom(RegionType.Set_All) 
                    && intVec.WithinRegions(pawn.Position, pawn.Map, 9, TraverseMode.NoPassClosedDoors, RegionType.Set_Passable))
                {
                    pawn.GetLord().Notify_ReachedDutyLocation(pawn);
                    return false;
                }
                Thing attackTarget = null;
                if (!intVec.IsValid)
                {
                    var pawnTargets = pawn.Map.attackTargetsCache.TargetsHostileToFaction(pawn.Faction).Where(x => !x.ThreatDisabled(pawn) 
                        && x.Thing.Faction == Faction.OfPlayer).Select(x => x.Thing);
                    var buildingTargets = pawn.Map.listerBuildings.allBuildingsColonist.Where(x => x.def.designationCategory?.defName != "Structure"
                        && x.def.designationCategory?.defName != "Security" && !x.def.IsFrame && x.HitPoints > 0 && x.def.altitudeLayer != AltitudeLayer.Conduits);

                    if (lastAttackTarget.ContainsKey(pawn.ThingID))
                    {
                        var lastAttackTargetForPawn = lastAttackTarget[pawn.ThingID];
                        if (buildingTargets.FirstOrDefault(x => x.ThingID == lastAttackTargetForPawn.ThingID) != null)
                        {
                            attackTarget = lastAttackTarget[pawn.ThingID];
                        }
                        else
                        {
                            lastAttackTarget.Remove(pawn.ThingID);
                        }
                    }
                    
                    if (attackTarget == null)
                    {
                        if (!buildingTargets.TryRandomElement(out attackTarget))
                        {
                            if (!pawnTargets.TryRandomElement(out attackTarget))
                            {
                                return false;
                            }
                        }
                    }

                    intVec = attackTarget.Position;
                }
                if (!pawn.CanReach(intVec, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.PassAllDestroyableThings))
                {
                    return false;
                }

                lastAttackTarget[pawn.ThingID] = attackTarget;

                var customTuning = new PathFinderCostTuning() { 
                    costOffLordWalkGrid = 0, 
                    costBlockedWallBase = 0, 
                    costBlockedWallExtraPerHitPoint = 5,
                    costBlockedDoor = 0,
                    costBlockedDoorPerHitPoint = 5,
                    costBlockedWallExtraForNaturalWalls = 0
                };

                //clear old
                pathCostCache.RemoveAll(x => Time.realtimeSinceStartup - x.Item5 > cacheExpiryTime);
                pathCostCache.RemoveAll(x => !(x.Item3?.Position.IsValid ?? false) || x.Item3.Position.GetRegion(pawn.Map, RegionType.Set_Passable) != null);
                Tuple<IntVec3, IntVec3, Thing, IntVec3, float> memoryValue = null;
                foreach (var cost in pathCostCache)
                {                    
                    if (cost.Item1.DistanceTo(pawn.Position) < cacheCellDistanceMax && cost.Item2.DistanceTo(attackTarget.Position) < cacheCellDistanceMax)
                    {
                        //Log.Message($"cachesize: {pathCostCache.Count} cacheTime: {Time.realtimeSinceStartup - cost.Item5} cost.Key: {pawn.Position} {cost.Item1.DistanceTo(pawn.Position)} " +
                        //$"{attackTarget.Position} {cost.Item2.DistanceTo(attackTarget.Position)}");
                        memoryValue = cost;
                        break;
                    }
                }

                if (memoryValue == null)
                {
                    using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, intVec,
                        TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings, false, true, false), PathEndMode.OnCell, customTuning))
                    {
                        IntVec3 cellBeforeBlocker;
                        Thing thing = pawnPath.FirstBlockingBuilding(out cellBeforeBlocker, pawn);
                        pathCostCache.Add(new Tuple<IntVec3, IntVec3, Thing, IntVec3, float>(pawn.Position, attackTarget.Position, thing, cellBeforeBlocker, Time.realtimeSinceStartup));
                        //Log.Message($"memory key: {memoryKey} {memoryCache[memoryKey][pawn.Position].Item1} {memoryCache[memoryKey][pawn.Position].Item2} {memoryCache[memoryKey][pawn.Position].Item3}");
                        __result = GetSapJob(__instance, pawn, thing, cellBeforeBlocker);
                    }
                }
                else if (memoryValue != null)
                {
                    __result = GetSapJob(__instance, pawn, memoryValue.Item3, memoryValue.Item4);
                }
                return false;
            }

            private static Job GetSapJob(RimWorld.JobGiver_AISapper __instance, Pawn pawn, Thing thing, IntVec3 cellBeforeBlocker)
            {
                if (thing != null)
                {
                    var job = DigUtility.PassBlockerJob(pawn, thing, cellBeforeBlocker, __instance.canMineMineables, __instance.canMineNonMineables);
                    return job;
                }
                else
                {
                    return JobMaker.MakeJob(JobDefOf.Goto, cellBeforeBlocker, 500, true);
                }
            }
        }
    }
}
