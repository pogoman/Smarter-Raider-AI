using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using Verse;
using Verse.AI.Group;
using Unity.Jobs;
using static UnityEngine.GraphicsBuffer;
using static PogoAI.Patches.JobGiver_AISapper;
using System;
using Verse.Noise;
using Unity.Collections;
using UnityEngine;
using System.Diagnostics.Eventing.Reader;
using System.Security.Cryptography;

namespace PogoAI.Patches
{
    public static class JobGiver_AISapper
    {
        public class CachedPath
        {
            public Pawn pawn;
            public IAttackTarget attackTarget;
            public Thing blockingThing;
            public IntVec3 cellBefore;
            public IntVec3 cellAfter;
            public IntVec3 cachedPosition;
            public int cachedCellCost;
            public List<int> excludeList = new List<int>();

            public CachedPath(Pawn pawn, IAttackTarget targetThing, Thing blockingThing, IntVec3 cellBefore, IntVec3 cellAfter)
            {
                this.pawn = pawn;
                this.attackTarget = targetThing;
                this.cachedPosition = targetThing.Thing.Position;
                this.cachedCellCost = Init.pathCostGrid[pawn.Map.cellIndices.CellToIndex(cachedPosition)];
                this.blockingThing = blockingThing;
                this.cellBefore = cellBefore;
                this.cellAfter = cellAfter;
            }
        }

        public class PogoCustomizer : PathRequest.IPathGridCustomizer, IDisposable
        {
            private NativeArray<ushort> grid;
            private Pawn pawn;
            public PogoCustomizer(Pawn pawn)
            {
                this.pawn = pawn;
            }

            public NativeArray<ushort> GetOffsetGrid()
            {
                this.grid = Init.pathCostGrid;

                foreach (var cached in pathCostCache)
                {
                    if (cached.blockingThing != null && cached.pawn != pawn)
                    {
                        this.grid[pawn.Map.cellIndices.CellToIndex(cached.blockingThing.Position)] = (ushort)10000;
#if DEBUG
                        Find.CurrentMap.debugDrawer.FlashCell(cached.blockingThing.Position, 0.5f, "NPB", 60); //Green
#endif
                    }
                }
                return this.grid;
            }

            public void Dispose() => this.grid.Dispose();
        }

        public static List<CachedPath> pathCostCache = new List<CachedPath>();

        public static bool findNewPaths = true;

        [HarmonyPatch(typeof(RimWorld.JobGiver_AISapper), "TryGiveJob")]
        public static class JobGiver_AISapper_TryGiveJob_Patch
        {
            public static bool Prefix(Pawn pawn, ref Job __result)
            {
                if (pawn.Faction == Faction.OfInsects || !pawn.Map.IsPlayerHome 
                    || pawn.mindState?.duty?.def == DutyDefOf.AssaultThing
                    || pawn.mindState?.duty?.def == DutyDefOf.PrisonerEscapeSapper)
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

                //                if (pathCostCache.RemoveAll(x => x.attackTarget.ThreatDisabled(pawn) || x.attackTarget.Thing.Destroyed 
                //                    || (x.blockingThing == null && !x.pawn.Position.WithinRegions(x.cellBefore, pawn.Map, 9, TraverseMode.NoPassClosedDoors, RegionType.Set_Passable))
                //                    || (x.blockingThing != null && !Utilities.CellBlockedFor(pawn, x.blockingThing.Position))
                //                    || x.cachedPosition != x.attackTarget.Thing.Position
                //                    || x.cachedCellCost != Init.pathCostGrid[pawn.Map.cellIndices.CellToIndex(x.cachedPosition)]) > 0)
                //                {
                //#if DEBUG
                //                    Log.Message($"{pawn} Cache trimmed: {string.Join(",", pathCostCache.Select(x => x.attackTarget.Thing))}");
                //#endif
                //                    findNewPaths = true;
                //                }

                //                CachedPath memoryValue = pathCostCache.OrderBy(x => pawn.Position.DistanceTo(x.cellBefore)).FirstOrDefault(x => 
                //                    x.blockingThing == null || pawn.HasReserved(x.blockingThing) || pawn.CanReserve(x.blockingThing));                
                //IAttackTarget attackTarget = null;
                var cacheIndex = -1;
                CachedPath memoryValue = null;
                //if (memoryValue != null)
                //{
                //    intVec = memoryValue.attackTarget.Thing.Position;
                //    attackTarget = memoryValue.attackTarget;
                //    cacheIndex = pathCostCache.IndexOf(memoryValue);
                //}

                //                if (memoryValue == null)
                //                {
                //                    attackTarget = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn)
                //                        .Where(x => !x.ThreatDisabled(pawn) && !x.Thing.Destroyed && x.Thing.Faction == Faction.OfPlayer && !pathCostCache.Any(y => y.pawn == x.Thing))
                //                        .OrderBy(x => Init.pathCostGrid[pawn.Map.cellIndices.CellToIndex(((Thing)x).Position)]).FirstOrDefault();

                //                    if (attackTarget == null)
                //                    {
                //                        findNewPaths = false;
                //                    }
                //                    else
                //                    {
                //                        intVec = attackTarget.Thing.Position;
                //#if DEBUG
                //                        Find.CurrentMap.debugDrawer.FlashCell(attackTarget.Thing.Position, 0.8f, $"{attackTarget.Thing}", 60);
                //#endif
                //                    }
                //                }

   
                if (findNewPaths && memoryValue == null && pathCostCache.Count <= Init.settings.maxSappers)
                {
                    //var attackTargets = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn)
                    //    .Where(x => !x.ThreatDisabled(pawn) && !x.Thing.Destroyed && x.Thing.Faction == Faction.OfPlayer && !pathCostCache.Any(y => y.pawn == x.Thing));

                    var attackTarget = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn)
                        .Where(x => !x.ThreatDisabled(pawn) && !x.Thing.Destroyed && x.Thing.Faction == Faction.OfPlayer && !pathCostCache.Any(y => y.pawn == x.Thing))
                        .OrderBy(x => ((Thing)x).Position.DistanceToSquared(pawn.Position)).FirstOrDefault();
                    intVec = attackTarget.Thing.Position;
                    var customizer = new PogoCustomizer(pawn);
                    PathFinderCostTuning tuning = new PathFinderCostTuning(70, 1f, 0, 50, 1f, 70, 300, 800);
                    using (PawnPath pawnPath = pawn.Map.pathFinder.FindPathNow(pawn.Position, intVec,
                        TraverseParms.For(pawn, Danger.None, TraverseMode.PassAllDestroyableThings, false, true, false), null, PathEndMode.OnCell, customizer))
                    {
                        var nodes = Traverse.Create(pawnPath).Field("nodes").GetValue<List<IntVec3>>();
                        IntVec3 cellBeforeBlocker = IntVec3.Invalid;
                        IntVec3 cellAfterBlocker = IntVec3.Invalid;
                        if (pawnPath != PawnPath.NotFound)
                        {
                            Thing blockingThing = pawnPath.FirstBlockingBuilding(out cellBeforeBlocker, pawn);
                            if (blockingThing == null && nodes.Count > 1)
                            {
                                if (!attackTarget.ThreatDisabled((IAttackTargetSearcher)pawn) && AttackTargetFinder.IsAutoTargetable(attackTarget)
                                    && (!(attackTarget.Thing is Pawn thing) || thing.IsCombatant() || GenSight.LineOfSightToThing(pawn.Position, (Thing)thing, pawn.Map)))
                                {
                                    Thing dest = (Thing)attackTarget;
                                    int squared = dest.Position.DistanceToSquared(pawn.Position);
                                    if (pawn.CanReach((LocalTargetInfo)dest, PathEndMode.OnCell, Danger.Deadly))
                                    {
#if DEBUG
                                        Find.CurrentMap.debugDrawer.FlashCell(dest.Position, 1f, $"ENGAGE", 300);
                                        Log.Message($"{pawn} targ: {attackTarget} engaging from loc {dest.Position}");
#endif
                                        __result = JobMaker.MakeJob(JobDefOf.Goto, (LocalTargetInfo)dest);
                                        __result.collideWithPawns = true;
                                        __result.expiryInterval = Rand.RangeInclusive(Init.settings.reactionMin, Init.settings.reactionMax);
                                        __result.checkOverrideOnExpire = true;
                                        __result.expireRequiresEnemiesNearby = false;
                                        return false;
                                    }
                                }
                                else
                                {
                                    cellBeforeBlocker = nodes[1];
#if DEBUG
                                    Log.Message($"{pawn} targ: {attackTarget} no blocker {cellBeforeBlocker} Cost: {pawnPath.TotalCost} Length: {nodes.Count}");
#endif
                                }
                            }
                            else
                            {
                                cellAfterBlocker = blockingThing.Position - cellBeforeBlocker + blockingThing.Position;
#if DEBUG
                                Find.CurrentMap.debugDrawer.FlashCell(blockingThing.Position, 1f, $"b{pawnPath.TotalCost}", 300);
                                Log.Message($"{pawn} targ: {attackTarget} blocked {blockingThing} cb: {cellBeforeBlocker} ca: {cellAfterBlocker} " +
                                    $"reg: {Utilities.CellBlockedFor(pawn, blockingThing.Position)} Cost: {pawnPath.TotalCost} Length: {nodes.Count}");
#endif
                            }
                            memoryValue = new CachedPath(pawn, attackTarget, blockingThing, cellBeforeBlocker, cellAfterBlocker);
                            //pathCostCache.Add(memoryValue);
#if DEBUG
                            Log.Message($"{pawn} targ: {attackTarget} added to cache. INDEX: {pathCostCache.Count - 1}");
#endif
                        }
                    }
                }
                else if (memoryValue == null)
                {
                    //if (attackTarget != null)
                    //{
                    //    var findTarget = pathCostCache.FirstOrDefault(x => x.attackTarget == attackTarget && x.blockingThing == null);
                    //    if (findTarget != null)
                    //    {
                    //        memoryValue = findTarget;
                    //    }
                    //}
                    //if (memoryValue == null)
                    //{
                    //    var sappingPawn = pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction).Where(x => x.CurJobDef == JobDefOf.Mine || x.CurJobDef == JobDefOf.AttackMelee)
                    //        .OrderBy(x => x.Position.DistanceTo(pawn.Position)).FirstOrDefault();
                    //    if (sappingPawn == null || pawn.Position.DistanceTo(sappingPawn.Position) < 10)
                    //    {
                    //        return false;
                    //    }
                    //    __result = JobMaker.MakeJob(JobDefOf.Follow, sappingPawn);
                    //}
                }

                if (memoryValue != null)
                {
                    __result = GetSapJob(pawn, memoryValue);
#if DEBUG
                    if (__result != null)
                    {
                        Find.CurrentMap.debugDrawer.FlashCell(pawn.Position, 0.8f, $"{cacheIndex}" +
                            $"\n{__result.def.defName.Substring(0, 3)}\n{__result.targetA.Cell.x},{__result.targetA.Cell.z}", 60); //blue
                    }
#endif
                }

                if (__result != null)
                {
                    __result.collideWithPawns = true;
                    __result.expiryInterval = Rand.RangeInclusive(Init.settings.reactionMin, Init.settings.reactionMax);
                    __result.ignoreDesignations = true;
                    __result.checkOverrideOnExpire = true;
                    __result.expireRequiresEnemiesNearby = false;
                }

                return false;
            }            

            private static Job GetSapJob(Pawn pawn, CachedPath memoryValue)
            {
                var cellBeforeBlocker = memoryValue.cellBefore;
                var blockingThing = memoryValue.blockingThing;
                if (memoryValue.blockingThing == null)
                {
                    blockingThing = memoryValue.attackTarget.Thing;
                }
                var distanceToTarget = pawn.Position.DistanceTo(cellBeforeBlocker);
                Job job = null;

                if (memoryValue.blockingThing == null)
                {
                    IntVec3 intVec = CellFinder.RandomClosewalkCellNear(blockingThing.Position, pawn.Map, 5, null);
                    job = JobMaker.MakeJob(JobDefOf.AttackMelee, blockingThing);
                }
                else
                {                   
                    if (memoryValue.blockingThing != null)
                    {
                        if (memoryValue.blockingThing.def.mineable && !StatDefOf.MiningSpeed.Worker.IsDisabledFor(pawn))
                        {
                            job = JobMaker.MakeJob(JobDefOf.Mine, blockingThing);
                        }
                        else
                        {
                            job = JobMaker.MakeJob(JobDefOf.AttackMelee, blockingThing);
                        }
                        if (pawn.CanReserve(blockingThing) && !pawn.HasReserved(blockingThing))
                        {
                            pawn.ClearAllReservations();
                            pawn.Reserve(blockingThing, job);
                        }
                    } 
                }               

                return job;
            }
        }
    }
}
