using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Verse;
using Verse.AI;
using Verse.AI.Group;

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
            public int cachedAvoid;

            public CachedPath(Pawn pawn, IAttackTarget targetThing, Thing blockingThing, IntVec3 cellBefore, IntVec3 cellAfter)
            {
                this.pawn = pawn;
                this.attackTarget = targetThing;
                this.blockingThing = blockingThing;
                this.cellBefore = cellBefore;
                this.cellAfter = cellAfter;
                this.cachedAvoid = cellAfter.IsValid ? pawn.Map.avoidGrid[cellAfter] : 0;
            }
        }

        public class AlreadyReservedCustomizer : PathRequest.IPathGridCustomizer, IDisposable
        {
            private NativeArray<ushort> grid;
            private readonly Pawn pawn;

            public AlreadyReservedCustomizer(Pawn pawn)
            {
                this.pawn = pawn;
            }

            public NativeArray<ushort> GetOffsetGrid()
            {
                if (!grid.IsCreated)
                {
                    grid = new NativeArray<ushort>(pawn.Map.cellIndices.NumGridCells, Allocator.Persistent);
                }
                var cache = PogoMapComponent.For(pawn.Map)?.pathCostCache;
                if (cache != null)
                {
                    foreach (var cached in cache)
                    {
                        if (cached.blockingThing != null && cached.pawn != pawn)
                        {
                            grid[pawn.Map.cellIndices.CellToIndex(cached.blockingThing.Position)] = 10000;
#if DEBUG
                            pawn.Map.debugDrawer.FlashCell(cached.blockingThing.Position, 0.5f, "NPB", 60); //Green
#endif
                        }
                    }
                }
                return grid;
            }

            public void Dispose()
            {
                if (grid.IsCreated)
                {
                    grid.Dispose();
                }
            }
        }

        [HarmonyPatch(typeof(RimWorld.JobGiver_AISapper), "TryGiveJob")]
        public static class JobGiver_AISapper_TryGiveJob_Patch
        {
            public static bool Prefix(Pawn pawn, ref Job __result)
            {
                try
                {
                    if (pawn.Faction == Faction.OfInsects || !pawn.Map.IsPlayerHome
                        || pawn.mindState?.duty == null
                        || pawn.mindState.duty.def == DutyDefOf.AssaultThing
                        || pawn.mindState.duty.def == DutyDefOf.PrisonerEscapeSapper)
                    {
                        return true;
                    }

                    var lord = pawn.GetLord();
                    var comp = PogoMapComponent.For(pawn.Map);
                    if (lord == null || comp == null)
                    {
                        return true;
                    }
                    var pathCostCache = comp.pathCostCache;

                    IntVec3 intVec = pawn.mindState.duty.focus.Cell;
                    if (intVec.IsValid && (float)intVec.DistanceToSquared(pawn.Position) < 100f && intVec.GetRoom(pawn.Map) == pawn.GetRoom(RegionType.Set_All)
                        && intVec.WithinRegions(pawn.Position, pawn.Map, 9, TraverseMode.NoPassClosedDoors, RegionType.Set_Passable))
                    {
                        lord.Notify_ReachedDutyLocation(pawn);
                        return false;
                    }

                    if (pathCostCache.RemoveAll(x => x.attackTarget.ThreatDisabled(pawn) || x.attackTarget.Thing.Destroyed
                        || (x.blockingThing == null && !x.pawn.Position.WithinRegions(x.cellBefore, pawn.Map, 9, TraverseMode.NoPassClosedDoors, RegionType.Set_Passable))
                        || (x.blockingThing != null && !Utilities.CellBlockedFor(pawn, x.blockingThing.Position))
                        || (x.cellAfter.IsValid && x.cachedAvoid != pawn.Map.avoidGrid[x.cellAfter])) > 0)
                    {
#if DEBUG
                        Log.Message($"{pawn} Cache trimmed: {string.Join(",", pathCostCache.Select(x => x.attackTarget.Thing))}");
#endif
                        comp.findNewPaths = true;
                    }

                    var memoryValue = pathCostCache.OrderBy(x => pawn.Position.DistanceTo(x.cellBefore)).FirstOrDefault(x =>
                        x.blockingThing == null || pawn.HasReserved(x.blockingThing) || pawn.CanReserve(x.blockingThing));
                    IAttackTarget attackTarget = null;
                    var cacheIndex = -1;

                    if (memoryValue != null)
                    {
                        intVec = memoryValue.attackTarget.Thing.Position;
                        attackTarget = memoryValue.attackTarget;
                        cacheIndex = pathCostCache.IndexOf(memoryValue);
                    }

                    if (memoryValue == null)
                    {
                        attackTarget = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn)
                            .Where(x => !x.ThreatDisabled(pawn) && !x.Thing.Destroyed && x.Thing.Faction == Faction.OfPlayer && !pathCostCache.Any(y => y.pawn == x.Thing))
                            .OrderBy(x => pawn.Map.avoidGrid[((Thing)x).Position]).ThenBy(x => ((Thing)x).Position.DistanceToSquared(pawn.Position)).FirstOrDefault();

                        if (attackTarget == null)
                        {
                            comp.findNewPaths = false;
                        }
                        else
                        {
                            intVec = attackTarget.Thing.Position;
#if DEBUG
                            pawn.Map.debugDrawer.FlashCell(attackTarget.Thing.Position, 0.8f, $"{attackTarget.Thing}", 60);
#endif
                        }
                    }

                    var maxSaps = lord.ownedPawns.Where(x => !x.Dead && !x.Destroyed).Count() / 2;
                    if (comp.findNewPaths && memoryValue == null && pathCostCache.Count <= (maxSaps > Init.settings.maxSappers ? Init.settings.maxSappers : maxSaps))
                    {
                        using (var customizer = new AlreadyReservedCustomizer(pawn))
                        using (PawnPath pawnPath = pawn.Map.pathFinder.FindPathNow(pawn.Position, intVec,
                            TraverseParms.For(pawn, Danger.None, TraverseMode.PassAllDestroyableThings, false, true, false), null, PathEndMode.OnCell, customizer))
                        {
                            if (pawnPath != PawnPath.NotFound)
                            {
                                var nodes = pawnPath.nodes;
                                IntVec3 cellBeforeBlocker = IntVec3.Invalid;
                                IntVec3 cellAfterBlocker = IntVec3.Invalid;
                                Thing blockingThing = pawnPath.FirstBlockingBuilding(out cellBeforeBlocker, pawn);
                                if (blockingThing == null)
                                {
                                    if (nodes.Count > 1)
                                    {
                                        if (!attackTarget.ThreatDisabled((IAttackTargetSearcher)pawn) && AttackTargetFinder.IsAutoTargetable(attackTarget)
                                            && (!(attackTarget.Thing is Pawn thing) || thing.IsCombatant() || GenSight.LineOfSightToThing(pawn.Position, (Thing)thing, pawn.Map)))
                                        {
                                            Thing dest = (Thing)attackTarget;
                                            if (pawn.CanReach((LocalTargetInfo)dest, PathEndMode.OnCell, Danger.Deadly))
                                            {
#if DEBUG
                                                pawn.Map.debugDrawer.FlashCell(dest.Position, 1f, $"ENGAGE", 300);
                                                Log.Message($"{pawn} targ: {attackTarget} engaging from loc {dest.Position}");
#endif
                                                __result = JobMaker.MakeJob(JobDefOf.Goto, (LocalTargetInfo)dest);
                                                __result.collideWithPawns = true;
                                                __result.expiryInterval = 500;
                                                __result.checkOverrideOnExpire = true;
                                                __result.expireRequiresEnemiesNearby = false;
                                                return false;
                                            }
                                        }
                                        cellBeforeBlocker = nodes[1];
#if DEBUG
                                        Log.Message($"{pawn} targ: {attackTarget} no blocker {cellBeforeBlocker} Cost: {pawnPath.TotalCost} Length: {nodes.Count}");
#endif
                                    }
                                    else
                                    {
                                        cellBeforeBlocker = pawn.Position;
                                    }
                                }
                                else
                                {
                                    cellAfterBlocker = blockingThing.Position - cellBeforeBlocker + blockingThing.Position;
#if DEBUG
                                    pawn.Map.debugDrawer.FlashCell(blockingThing.Position, 1f, $"b{pawnPath.TotalCost}", 300);
                                    Log.Message($"{pawn} targ: {attackTarget} blocked {blockingThing} cb: {cellBeforeBlocker} ca: {cellAfterBlocker} " +
                                        $"reg: {Utilities.CellBlockedFor(pawn, blockingThing.Position)} Cost: {pawnPath.TotalCost} Length: {nodes.Count}");
#endif
                                }
                                memoryValue = new CachedPath(pawn, attackTarget, blockingThing, cellBeforeBlocker, cellAfterBlocker);
                                pathCostCache.Add(memoryValue);
#if DEBUG
                                Log.Message($"{pawn} targ: {attackTarget} added to cache. INDEX: {pathCostCache.Count - 1}");
#endif
                            }
                        }
                    }
                    else if (memoryValue == null)
                    {
                        if (attackTarget != null)
                        {
                            var findTarget = pathCostCache.FirstOrDefault(x => x.attackTarget == attackTarget && x.blockingThing == null);
                            if (findTarget != null)
                            {
                                memoryValue = findTarget;
                            }
                        }
                        if (memoryValue == null)
                        {
                            var sappingPawn = pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction).Where(x => x.CurJobDef == JobDefOf.Mine || x.CurJobDef == JobDefOf.AttackMelee)
                                .OrderBy(x => x.Position.DistanceTo(pawn.Position)).FirstOrDefault();
                            if (sappingPawn == null || pawn.Position.DistanceTo(sappingPawn.Position) < 10)
                            {
                                return false;
                            }
                            __result = JobMaker.MakeJob(JobDefOf.Follow, sappingPawn);
                        }
                    }

                    if (memoryValue != null)
                    {
                        __result = GetSapJob(pawn, memoryValue);
#if DEBUG
                        if (__result != null)
                        {
                            pawn.Map.debugDrawer.FlashCell(pawn.Position, 0.8f, $"{cacheIndex}" +
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
                    else
                    {
                        Log.Message($"SRAI: Pawn {pawn} no sap job found, defaulting to vanilla logic");
                        return true;
                    }

                    return false;
                }
                catch (Exception e)
                {
                    Log.Error($"SRAI Exception: {e.StackTrace}");
                    return true;
                }
            }

            private static Job GetSapJob(Pawn pawn, CachedPath memoryValue)
            {
                Job job;
                if (memoryValue.blockingThing == null)
                {
                    job = JobMaker.MakeJob(JobDefOf.AttackMelee, memoryValue.attackTarget.Thing);
                }
                else
                {
                    var blockingThing = memoryValue.blockingThing;
                    if (blockingThing.def.mineable && !StatDefOf.MiningSpeed.Worker.IsDisabledFor(pawn))
                    {
                        job = JobMaker.MakeJob(JobDefOf.Mine, blockingThing);
                    }
                    else
                    {
                        job = JobMaker.MakeJob(JobDefOf.AttackMelee, blockingThing);
                    }
                    if (pawn.CanReserve(blockingThing) && !pawn.HasReserved(blockingThing))
                    {
                        pawn.Reserve(blockingThing, job);
                    }
                }
                return job;
            }
        }
    }
}
