using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using Verse;
using Verse.AI.Group;
using Unity.Jobs;
using static UnityEngine.GraphicsBuffer;
using Steamworks;

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
            public List<int> excludeList = new List<int>();

            public CachedPath(Pawn pawn, IAttackTarget targetThing, Thing blockingThing, IntVec3 cellBefore, IntVec3 cellAfter)
            {
                this.pawn = pawn;
                this.attackTarget = targetThing;
                this.blockingThing = blockingThing;
                this.cellBefore = cellBefore;
                this.cellAfter = cellAfter;
            }
        }

        public class CustomTuning : PathFinderCostTuning.ICustomizer
        {
            Pawn pawn;

            public CustomTuning(Pawn pawn)
            {
                this.pawn = pawn;
            }

            public int CostOffset(IntVec3 from, IntVec3 to)
            {
                if (pawn != null)
                {
                    var edifice = from.GetEdifice(pawn.Map);
                    if (edifice != null && Utilities.CellBlockedFor(pawn, from) && !pawn.CanReserve(edifice))
                    {
#if DEBUG
                        Find.CurrentMap.debugDrawer.FlashCell(from, 0.5f, "NPB", 60); //Green
#endif
                        return 10000;
                    }
                }
                return 0;
            }

        }

        public static PathFinderCostTuning customTuning = new PathFinderCostTuning() {};

        public static List<CachedPath> pathCostCache = new List<CachedPath>();

        public static bool findNewPaths = true;

        [HarmonyPatch(typeof(RimWorld.JobGiver_AISapper), "TryGiveJob")]
        public static class JobGiver_AISapper_TryGiveJob_Patch
        {
            public static bool Prefix(Pawn pawn, ref Job __result)
            {
                if (pawn.Faction == Faction.OfInsects || !pawn.Map.IsPlayerHome)
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

                if (pathCostCache.RemoveAll(x => x.attackTarget.ThreatDisabled(pawn) || x.attackTarget.Thing.Destroyed 
                    || (x.blockingThing != null && !Utilities.CellBlockedFor(pawn, x.blockingThing.Position))) > 0)
                {
#if DEBUG
                    Log.Message($"{pawn} Cache trimmed: {string.Join(",", pathCostCache.Select(x => x.attackTarget.Thing))}");
#endif
                    findNewPaths = true;
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
                        .Where(x => !x.ThreatDisabled(pawn) && !x.Thing.Destroyed && x.Thing.Faction == Faction.OfPlayer).RandomElement();

                    if (attackTarget == null)
                    {
                        return false;
                    }
                    intVec = attackTarget.Thing.Position;
#if DEBUG
                    Find.CurrentMap.debugDrawer.FlashCell(attackTarget.Thing.Position, 0.8f, $"{attackTarget.Thing}", 60);
#endif
                }
                    
                if (memoryValue == null && findNewPaths && pathCostCache.Count <= Init.settings.maxSappers)
                {
                    customTuning.custom = new CustomTuning(pawn);
                    using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, intVec,
                        TraverseParms.For(pawn, Danger.None, TraverseMode.PassAllDestroyableThings, false, true, false), PathEndMode.OnCell, customTuning))
                    {
                        IntVec3 cellBeforeBlocker = IntVec3.Invalid;
                        IntVec3 cellAfterBlocker = IntVec3.Invalid;
                        Thing blockingThing = pawnPath.FirstBlockingBuilding(out cellBeforeBlocker, pawn);
                        if (blockingThing == null && pawnPath.nodes.Count > 1)
                        {
                            cellBeforeBlocker = pawnPath.nodes[1];
#if DEBUG
                            Log.Message($"{pawn} targ: {attackTarget} no blocker {cellBeforeBlocker} Cost: {pawnPath.TotalCost} Length: {pawnPath.nodes.Count}");
#endif
                        }
                        else
                        {
                            cellAfterBlocker = blockingThing.Position - cellBeforeBlocker + blockingThing.Position;
#if DEBUG
                            Find.CurrentMap.debugDrawer.FlashCell(blockingThing.Position, 1f, $"b{pawnPath.TotalCost}", 300);
                            Log.Message($"{pawn} targ: {attackTarget} blocked {blockingThing} cb: {cellBeforeBlocker} ca: {cellAfterBlocker} " +
                                $"reg: {Utilities.CellBlockedFor(pawn, blockingThing.Position)} Cost: {pawnPath.TotalCost} Length: {pawnPath.nodes.Count}");
#endif
                        }
                        memoryValue = pathCostCache.FirstOrDefault(x => x.cellBefore == cellBeforeBlocker);
                        if (memoryValue == null)
                        {
                            memoryValue = new CachedPath(pawn, attackTarget, blockingThing, cellBeforeBlocker, cellAfterBlocker);
                            pathCostCache.Add(memoryValue);
#if DEBUG
                            Log.Message($"{pawn} targ: {attackTarget} added to cache. INDEX: {pathCostCache.Count - 1}");
#endif
                        }
                        else
                        {
                            findNewPaths = false;
                            memoryValue = null;
#if DEBUG
                            Log.Message($"{pawn} targ: {attackTarget} already cached disable findNew");
#endif
                        }
                    }
                }
                else if (memoryValue == null)
                {
                    var findTarget = pathCostCache.FirstOrDefault(x => x.attackTarget == attackTarget && x.blockingThing == null);
                    if (findTarget != null)
                    {
                        memoryValue = findTarget;
                    }
                    else
                    {
                        var sappingPawn = pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction).Where(x => x.CurJobDef == JobDefOf.Mine || x.CurJobDef == JobDefOf.AttackMelee)
                            .OrderBy(x => x.Position.DistanceTo(pawn.Position)).FirstOrDefault();
                        if (sappingPawn == null || pawn.Position.DistanceTo(sappingPawn.Position) < 20)
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
                        Find.CurrentMap.debugDrawer.FlashCell(pawn.Position, 0.8f, $"{cacheIndex}" +
                            $"\n{__result.def.defName.Substring(0, 3)}\n{__result.targetA.Cell.x},{__result.targetA.Cell.z}", 60); //blue
                    }
#endif
                }

                if (__result != null)
                {
                    __result.collideWithPawns = true;
                    __result.expiryInterval = Rand.RangeInclusive(120, 240);
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
                        if (!pawn.HasReserved(blockingThing))
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
