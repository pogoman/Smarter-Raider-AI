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
using PogoAI.Extensions;
using System.Security.Cryptography;

namespace PogoAI.Patches
{
    public static class JobGiver_AISapper
    {
        const int cacheExpiryTicks = 5; //Seconds
        const int minPathLengthToCache = 0;

        /// <summary>
        /// distance to reuse pathcost for similarly located targets
        /// </summary>
        const int cacheTargetCellDistanceMax = 10;

        /// <summary>
        /// distance to reuse pathcost for similarly sourced pawns
        /// </summary>
        const int cacheSourceCellDistanceMax = 10;

        static PathFinderCostTuning customTuning = new PathFinderCostTuning()
        {
            costOffLordWalkGrid = 0,
            costBlockedWallBase = 0,
            costBlockedWallExtraPerHitPoint = 5,
            costBlockedDoor = 0,
            costBlockedDoorPerHitPoint = 5,
            costBlockedWallExtraForNaturalWalls = 0
        };

        public class CachedPath
        {
            public Pawn pawn;
            public Thing targetThing;
            public IntVec3 cellBefore;
            public IntVec3 cellAfter;

            public CachedPath(Pawn pawn, Thing targetThing, IntVec3 cellBefore, IntVec3 cellAfter)
            {
                this.pawn = pawn;
                this.targetThing = targetThing;
                this.cellBefore = cellBefore;
                this.cellAfter = cellAfter;
            }
        }
        
        public static List<CachedPath> pathCostCache = new List<CachedPath>();

        [HarmonyPatch(typeof(RimWorld.JobGiver_AISapper), "TryGiveJob")]
        public static class JobGiver_AISapper_TryGiveJob_Patch
        {
            public static bool Prefix(Pawn pawn, ref Job __result)
            {
                if (pawn.Faction == Faction.OfInsects)
                {
                    return true;
                }
                IntVec3 intVec = pawn.mindState.duty.focus.Cell;
                if (intVec.IsValid && (float)intVec.DistanceToSquared(pawn.Position) < 100f && intVec.GetRoom(pawn.Map) == pawn.GetRoom(RegionType.Set_All) 
                    && intVec.WithinRegions(pawn.Position, pawn.Map, 9, TraverseMode.NoPassClosedDoors, RegionType.Set_Passable))
                {
                    pawn.GetLord().Notify_ReachedDutyLocation(pawn);
                    return false;
                }

                pathCostCache.RemoveAll(x =>/* Find.TickManager.TicksGame - x.Item7 > cacheExpiryTicks * 60 || */
                    !(x.targetThing?.Position.IsValid ?? false) || x.targetThing.Position.GetRegion(pawn.Map, RegionType.Set_Passable) != null
                    || (pawn.Position.DistanceTo(x.cellAfter) < 10 && pawn.CanReach(x.cellAfter, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.ByPawn)));

                CachedPath memoryValue = pathCostCache.FirstOrDefault();
                if (memoryValue != null)
                {
                    intVec = memoryValue.targetThing.Position;
                }

                if (!intVec.IsValid)
                {
                    Thing attackTarget;
                    //var targets = pawn.Map.attackTargetsCache.TargetsHostileToFaction(pawn.Faction).Where(x => !x.ThreatDisabled(pawn)
                    //    && x.Thing.Faction == Faction.OfPlayer && !blockedPositions.Any(y => x.Thing.Position == y)).Select(x => x.Thing).ToList();
                    attackTarget = pawn.Map.listerBuildings.allBuildingsColonist.OrderBy(x => x.Position.DistanceTo(pawn.Position)).Where(x => x.def.designationCategory != DesignationCategoryDefOf.Structure
                        && x.def.designationCategory != DesignationCategoryDefOf.Security && !x.def.IsFrame && x.HitPoints > 0 && x.def.altitudeLayer != AltitudeLayer.Conduits
                       /* && !x.IsBurning() && !blockedPositions.Any(y => x.Position == y)*/).FirstOrDefault();

                    if (attackTarget == null)
                    {
                        return false;
                    }
                    intVec = attackTarget.Position;
                    Find.CurrentMap.debugDrawer.FlashCell(intVec, 0.7f, "AT", 500);
                }

                if (memoryValue == null)
                {
                    memoryValue = pathCostCache.FirstOrDefault(x => x.targetThing.Position == intVec);
                }

                if (memoryValue == null)
                {                   
                    using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, intVec,
                        TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings, false, false, false), PathEndMode.OnCell, customTuning))
                    {
                        IntVec3 cellBeforeBlocker;
                        IntVec3 cellAfterBlocker;
                        Thing thing = FirstBlockingBuilding(pawnPath, out cellBeforeBlocker, out cellAfterBlocker, pawn);
                        IntVec3 cellBeforeTarget = IntVec3.Invalid;
                        if (pawnPath.nodes.Count > 1)
                        {
                            cellBeforeTarget = pawnPath.nodes[1];
                            //meleePathBlocked = PawnUtility.AnyPawnBlockingPathAt(cellBeforeTarget, pawn, true, false, false);
                        }
                        if (pawnPath.nodes?.Count >= minPathLengthToCache)
                        {
                            pathCostCache.Add(new CachedPath(pawn, thing, cellBeforeBlocker, cellAfterBlocker));
                        }
                        __result = GetSapJob( pawn, thing, cellBeforeBlocker);
                    }
                }
                else
                {
                    //pathCostCache.Add(new CachedPath(pawn, memoryValue.targetThing, memoryValue.cellBefore, memoryValue.cellAfter));
                    __result = GetSapJob(pawn, memoryValue.targetThing, memoryValue.cellBefore);
                }
                //Log.Message($"{pawn} {__result} {__result.targetA.Cell} {__result.def} {__result.expiryInterval}");
                return false;
            }

            public static Thing FirstBlockingBuilding(PawnPath path, out IntVec3 cellBefore, out IntVec3 cellAfter, Pawn pawn)
            {
                cellBefore = IntVec3.Invalid;
                cellAfter = IntVec3.Invalid;
                if (!path.Found)
                {
                    return null;
                }
                List<IntVec3> nodesReversed = path.NodesReversed;
                if (nodesReversed.Count == 1)
                {
                    cellBefore = nodesReversed[0];
                    return null;
                }
                Building building = null;
                IntVec3 intVec = IntVec3.Invalid;
                for (int i = nodesReversed.Count - 2; i >= 0; i--)
                {
                    Find.CurrentMap.debugDrawer.FlashCell(nodesReversed[i], 0.5f, "block", 500);
                    Building edifice = nodesReversed[i].GetEdifice(pawn.Map);
                    if (edifice != null)
                    {
                        Building_Door building_Door = edifice as Building_Door;
                        bool flag = building_Door != null && !building_Door.FreePassage && !building_Door.PawnCanOpen(pawn);
                        bool flag2 = edifice.def.IsFence && !pawn.def.race.CanPassFences;
                        if (flag || flag2 || edifice.def.passability == Traversability.Impassable)
                        {
                            if (building != null)
                            {
                                cellBefore = intVec;
                                return building;
                            }
                            cellBefore = nodesReversed[i + 1];
                            if (i - 1 > 0)
                            {
                                cellAfter = nodesReversed[i - 1];
                            }
                            return edifice;
                        }
                    }
                    if (edifice != null && edifice.def.passability == Traversability.PassThroughOnly && edifice.def.Fillage == FillCategory.Full)
                    {
                        if (building == null)
                        {
                            building = edifice;
                            intVec = nodesReversed[i + 1];
                        }
                    }
                    else if (edifice == null || edifice.def.passability != Traversability.PassThroughOnly)
                    {
                        building = null;
                    }
                }
                cellBefore = nodesReversed[0];
                return null;
            }

            private static Job GetSapJob(Pawn pawn, Thing thing, IntVec3 cellBeforeBlocker)
            {
                Job job = null;
                if (thing != null)
                {
                    if (!StatDefOf.MiningSpeed.Worker.IsDisabledFor(pawn))
                    {
                        job = DigUtility.MineOrWaitJob(pawn, thing, cellBeforeBlocker);
                    }
                    else
                    {
                        job = DigUtility.MeleeOrWaitJob(pawn, thing, cellBeforeBlocker);
                    }
                    if (job != null)
                    {
                        job.expiryInterval = Rand.RangeInclusive(502, 1800);
                        Find.CurrentMap.debugDrawer.FlashCell(thing.Position, 0.5f, "Targ", job.expiryInterval);
                        if (job.def == JobDefOf.Wait || (job.def == JobDefOf.Goto && job.targetA.Cell.DistanceTo(pawn.Position) < 15))
                        {
                            Building trashTarget = null;
                            for (int i = 0; i < GenRadial.NumCellsInRadius(10); i++)
                            {
                                IntVec3 c = pawn.Position + GenRadial.RadialPattern[i];
                                if (c.InBounds(pawn.Map))
                                {
                                    var edifice = c.GetEdifice(pawn.Map);
                                    if (edifice != null && (edifice.def == ThingDefOf.Wall || edifice.def == ThingDefOf.Door 
                                        || edifice.def.defName.Matches("embrasure")) && GenSight.LineOfSight(pawn.Position, c, pawn.Map)) 
                                    {
                                        using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, edifice,
                                            TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false, false, false), PathEndMode.Touch, null))
                                        {
                                            if (pawnPath.NodesLeftCount > 1 && pawnPath.nodes.Any(x => PawnUtility.AnyPawnBlockingPathAt(x, pawn, true, false, false)))
                                            {
                                                continue;
                                            }
                                            else
                                            {
                                                trashTarget = edifice;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            if (trashTarget != null)
                            {
                                Find.CurrentMap.debugDrawer.FlashCell(trashTarget.Position, 0.6f, $"{pawn} smash", 120);
                                job = TrashUtility.TrashJob(pawn, trashTarget, true, false);
                                if (job != null)
                                {
                                    job.expireRequiresEnemiesNearby = false;
                                    job.expiryInterval = 120;
                                    job.collideWithPawns = true;
                                }
                            }
                        }
                    }
                }
                if (job == null)
                {
                    Log.Message($"{pawn} job null");
                }
                return job;
            }
        }
    }
}
