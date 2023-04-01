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
using Mono.Unix.Native;

namespace PogoAI.Patches
{
    public static class JobGiver_AISapper
    {
        const int cacheExpiryTicks = 5; //Seconds
        const int minPathLengthToCache = 20;

        /// <summary>
        /// distance to reuse pathcost for similarly located targets
        /// </summary>
        const int cacheTargetCellDistanceMax = 10;

        /// <summary>
        /// distance to reuse pathcost for similarly sourced pawns
        /// </summary>
        const int cacheSourceCellDistanceMax = 10;

        /// <summary>
        /// pawn pos, target pos, blocking thing, cell before, cache time
        /// </summary>
        static List<Tuple<IntVec3, IntVec3, Thing, IntVec3, bool, int>> pathCostCache = new List<Tuple<IntVec3, IntVec3, Thing, IntVec3, bool, int>>();

        [HarmonyPatch(typeof(RimWorld.JobGiver_AISapper), "TryGiveJob")]
        public static class JobGiver_AISapper_TryGiveJob_Patch
        {
            public static bool Prefix(Pawn pawn, ref Job __result)
            {
                IntVec3 intVec = pawn.mindState.duty.focus.Cell;
                if (intVec.IsValid && (float)intVec.DistanceToSquared(pawn.Position) < 100f && intVec.GetRoom(pawn.Map) == pawn.GetRoom(RegionType.Set_All) 
                    && intVec.WithinRegions(pawn.Position, pawn.Map, 9, TraverseMode.NoPassClosedDoors, RegionType.Set_Passable))
                {
                    pawn.GetLord().Notify_ReachedDutyLocation(pawn);
                    return false;
                }
                Thing attackTarget = null;
                pathCostCache.RemoveAll(x => Find.TickManager.TicksGame - x.Item6 > cacheExpiryTicks * 60);
                pathCostCache.RemoveAll(x => !(x.Item3?.Position.IsValid ?? false) || x.Item3.Position.GetRegion(pawn.Map, RegionType.Set_Passable) != null);
                var blockedPositions = pathCostCache.Where(x => x.Item5).Select(x => x.Item2);
                if (!intVec.IsValid)
                {
                    var targets = pawn.Map.attackTargetsCache.TargetsHostileToFaction(pawn.Faction).Where(x => !x.ThreatDisabled(pawn)
                        && x.Thing.Faction == Faction.OfPlayer && !blockedPositions.Any(y => x.Thing.Position == y)).Select(x => x.Thing).ToList();
                    targets.AddRange(pawn.Map.listerBuildings.allBuildingsColonist.Where(x => x.def.designationCategory != DesignationCategoryDefOf.Structure
                        && x.def.designationCategory != DesignationCategoryDefOf.Security && !x.def.IsFrame && x.HitPoints > 0 && x.def.altitudeLayer != AltitudeLayer.Conduits
                        && !x.IsBurning() && !blockedPositions.Any(y => x.Position == y)));                   
                    
                    if (!targets.TryRandomElement(out attackTarget))
                    {
                        return false;
                    }

                    intVec = attackTarget.Position;
                }
                if (!pawn.CanReach(intVec, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.PassAllDestroyableThings))
                {
                    return false;
                }
                
                var customTuning = new PathFinderCostTuning() { 
                    costOffLordWalkGrid = 0, 
                    costBlockedWallBase = 0, 
                    costBlockedWallExtraPerHitPoint = 5,
                    costBlockedDoor = 0,
                    costBlockedDoorPerHitPoint = 5,
                    costBlockedWallExtraForNaturalWalls = 0
                };
                
                Tuple<IntVec3, IntVec3, Thing, IntVec3, bool, int> memoryValue = null;
                foreach (var cost in pathCostCache)
                {                    
                    if (cost.Item1.DistanceTo(pawn.Position) < cacheSourceCellDistanceMax && cost.Item2.DistanceTo(attackTarget.Position) < cacheTargetCellDistanceMax)
                    {
                        memoryValue = cost;
                        break;
                    }
                }

                if (memoryValue == null)
                {
                    using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, intVec,
                        TraverseParms.For(pawn, Danger.None, TraverseMode.PassAllDestroyableThings, false, false, false), PathEndMode.OnCell, customTuning))
                    {
                        IntVec3 cellBeforeBlocker;
                        Thing thing = pawnPath.FirstBlockingBuilding(out cellBeforeBlocker, pawn);
                        IntVec3 cellBeforeTarget = IntVec3.Invalid;
                        var meleePathBlocked = false;
                        if (pawnPath.nodes.Count > 1)
                        {
                            cellBeforeTarget = pawnPath.nodes[1];
                            meleePathBlocked = PawnUtility.AnyPawnBlockingPathAt(cellBeforeTarget, pawn, true, false, false);
                        }
                        if (pawnPath.nodes.Count >= minPathLengthToCache)
                        {
                            pathCostCache.Add(new Tuple<IntVec3, IntVec3, Thing, IntVec3, bool, int>(pawn.Position, attackTarget.Position,
                                thing, cellBeforeBlocker, meleePathBlocked, Find.TickManager.TicksGame));
                        }
                        __result = GetSapJob( pawn, thing, cellBeforeBlocker, intVec, meleePathBlocked);
                    }
                }
                else if (memoryValue != null)
                {
                    __result = GetSapJob(pawn, memoryValue.Item3, memoryValue.Item4, intVec, memoryValue.Item5);
                }
                return false;
            }

            private static Job GetSapJob(Pawn pawn, Thing thing, IntVec3 cellBeforeBlocker, IntVec3 intVec, bool meleePathBlocked)
            {
                Job job = null;
                if (thing != null)
                {
                    job = DigUtility.PassBlockerJob(pawn, thing, cellBeforeBlocker, true, true);
                    if (job != null)
                    {
                        job.expiryInterval = Rand.RangeInclusive(502, 1800);
                    }
                }
                if (job == null)
                {
                    pathCostCache.RemoveAll(x => x.Item4 == cellBeforeBlocker);
                    if (!(pawn.equipment.PrimaryEq?.PrimaryVerb?.IsMeleeAttack ?? true) || !meleePathBlocked)
                    {
                        job = JobMaker.MakeJob(JobDefOf.Goto, intVec, 501, true);
                        job.collideWithPawns = true;
                    }
                    else
                    {
                        Log.Message($"{pawn} blocked");
                    }
                }
                return job;
            }
        }
    }
}
