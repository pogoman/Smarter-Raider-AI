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
    internal class JobGiver_AISapper
    {
        const int cacheExpiryTicks = 5; //Divide 60 so roughly in seconds depending on fps and speed

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
        static List<Tuple<IntVec3, IntVec3, Thing, IntVec3, int>> pathCostCache = new List<Tuple<IntVec3, IntVec3, Thing, IntVec3, int>>();

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
                    
                    if (!buildingTargets.TryRandomElement(out attackTarget))
                    {
                        if (!pawnTargets.TryRandomElement(out attackTarget))
                        {
                            return false;
                        }
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
                
                pathCostCache.RemoveAll(x => Find.TickManager.TicksGame - x.Item5 > cacheExpiryTicks*60);
                pathCostCache.RemoveAll(x => !(x.Item3?.Position.IsValid ?? false) || x.Item3.Position.GetRegion(pawn.Map, RegionType.Set_Passable) != null);
                Tuple<IntVec3, IntVec3, Thing, IntVec3, int> memoryValue = null;
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
                        TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings, false, true, false), PathEndMode.OnCell, customTuning))
                    {
                        IntVec3 cellBeforeBlocker;
                        Thing thing = pawnPath.FirstBlockingBuilding(out cellBeforeBlocker, pawn);
                        pathCostCache.Add(new Tuple<IntVec3, IntVec3, Thing, IntVec3, int>(pawn.Position, attackTarget.Position, thing, cellBeforeBlocker, Find.TickManager.TicksGame));
                        __result = GetSapJob(__instance, pawn, thing, cellBeforeBlocker, intVec);
                    }
                }
                else if (memoryValue != null)
                {
                    __result = GetSapJob(__instance, pawn, memoryValue.Item3, memoryValue.Item4, intVec);
                }
                return false;
            }

            private static Job GetSapJob(RimWorld.JobGiver_AISapper __instance, Pawn pawn, Thing thing, IntVec3 cellBeforeBlocker, IntVec3 intVec)
            {
                Job job = null;
                if (thing != null)
                {
                    job = DigUtility.PassBlockerJob(pawn, thing, cellBeforeBlocker, __instance.canMineMineables, __instance.canMineNonMineables);
                    if (job.def == JobDefOf.Wait || (job.def == JobDefOf.AttackMelee && !pawn.CanReach(cellBeforeBlocker, PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn)))
                    {
                        job = null;
                    }
                }
                if (job == null)
                {
                    pathCostCache.RemoveAll(x => x.Item4 == cellBeforeBlocker);
                    job = JobMaker.MakeJob(JobDefOf.Goto, intVec, 500, true);
                }
                job.expiryInterval = Rand.RangeInclusive(450, 1800);
                return job;
            }
        }
    }
}
