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
            public float pathCost;
            public List<int> excludeList = new List<int>();

            public CachedPath(Pawn pawn, IAttackTarget targetThing, Thing blockingThing, IntVec3 cellBefore, float pathCost)
            {
                this.pawn = pawn;
                this.attackTarget = targetThing;
                this.blockingThing = blockingThing;
                this.cellBefore = cellBefore;
                this.pathCost = pathCost;
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
                        return 1000;
                    }
                    //else if (edifice != null && !Utilities.IsRoomWall(edifice))
                    //{
                    //    return 10000;
                    //}
//                    else if (pawn.Map.thingGrid.ThingAt(from, ThingCategory.Pawn)?.Faction == pawn.Faction)
//                    {
//                        return 5000;
//#if DEBUG
//                        Find.CurrentMap.debugDrawer.FlashCell(from, 0.5f, "NPP", 60); //Green
//#endif
//                    }
                }
                return 0;
            }

        }

        public static PathFinderCostTuning customTuning = new PathFinderCostTuning()
        {
            costOffLordWalkGrid = 0,
            costBlockedWallBase = 0,
            costBlockedWallExtraPerHitPoint = 5,
            costBlockedDoor = 0,
            costBlockedDoorPerHitPoint = 5,
            costBlockedWallExtraForNaturalWalls = 0
        };

        public static List<CachedPath> pathCostCache = new List<CachedPath>();

        public static bool findNewPaths = true;

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

                var potentials = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn).Where(x => !x.ThreatDisabled(pawn) && !x.Thing.Destroyed && x.Thing.Faction == Faction.OfPlayer);

                if (pathCostCache.RemoveAll(x => x.attackTarget.ThreatDisabled(pawn) || x.attackTarget.Thing.Destroyed 
                    || (x.blockingThing != null && (!Utilities.CellBlockedFor(pawn, x.blockingThing.Position) 
                    || Utilities.RoomIsBreached(pawn, x.attackTarget.Thing.Position)))
                    //&& Utilities.GetPawnsInRadius(pawn.Map, x.attackTarget.Thing.Position, 1, IntVec3.Invalid, true).Count(x => x.Faction == pawn.Faction) == 0
                    //|| Utilities.ThingBlocked(x.attackTarget.Thing, IntVec3.Invalid, true)
                    //|| (x.cellAfter != IntVec3.Invalid && pawn.Position.DistanceTo(x.cellAfter) < 5 
                    //    && pawn.CanReach(x.cellAfter, PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn))                    
                    ) > 0)
                {
#if DEBUG
                    Log.Message($"{pawn} Cache trimmed, amt left: {pathCostCache.Count}");
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
                    attackTarget = potentials.OrderBy(x => pawn.Map.avoidGrid.grid[x.Thing.Position]).ThenBy(x => x.Thing.Position.DistanceTo(pawn.Position))
                        .FirstOrDefault();

                    if (attackTarget == null)
                    {
                        return false;
                    }
                    intVec = attackTarget.Thing.Position;
#if DEBUG
                    Find.CurrentMap.debugDrawer.FlashCell(attackTarget.Thing.Position, 0.8f, $"{attackTarget.Thing}", 60);
#endif
                }
                    
                if (memoryValue == null && findNewPaths)
                {
                    customTuning.custom = new CustomTuning(pawn);
                    using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, intVec,
                        TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings, false, true, false), PathEndMode.OnCell, customTuning))
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
                            Find.CurrentMap.debugDrawer.FlashCell(blockingThing.Position, 1f, $"b{pawnPath.TotalCost}", 60);
                            Log.Message($"{pawn} targ: {attackTarget} blocked {blockingThing} cb: {cellBeforeBlocker} ca: {cellAfterBlocker} " +
                                $"reg: {Utilities.CellBlockedFor(pawn, blockingThing.Position)} Cost: {pawnPath.TotalCost} Length: {pawnPath.nodes.Count}");
#endif
                        }
                        memoryValue = pathCostCache.FirstOrDefault(x => x.cellBefore == cellBeforeBlocker);
                        if (memoryValue == null)
                        {
                            memoryValue = new CachedPath(pawn, attackTarget, blockingThing, cellBeforeBlocker, pawnPath.TotalCost);
                            pathCostCache.Add(memoryValue);
#if DEBUG
                            Log.Message($"{pawn} targ: {attackTarget} added to cache. INDEX: {pathCostCache.Count - 1}");
#endif
                        }
                        else
                        {
                            findNewPaths = false;
#if DEBUG
                            Log.Message($"{pawn} targ: {attackTarget} already cached disable findNew");
#endif
                        }
                    }
                }
                else if (memoryValue == null)
                {
                    var randomPawn = pawn.GetLord().ownedPawns.Where(x => x.CurJob != null && x.CurJobDef != JobDefOf.Follow).RandomElement();
                    __result = JobMaker.MakeJob(JobDefOf.Follow, randomPawn);

                    //NEXT IF CLOSE THEN WANDER
                }

                if (memoryValue != null)
                {
                    __result = GetSapJob(pawn, memoryValue);
#if DEBUG
                    if (__result != null)
                    {
                        Find.CurrentMap.debugDrawer.FlashCell(pawn.Position, 0.8f, $"{cacheIndex}" +
                            $"\n{__result.def.defName.Substring(0, 3)}\n{__result.targetA.Cell.x},{__result.targetA.Cell.z}", 60); //blue
                        Find.CurrentMap.debugDrawer.FlashCell(pawn.Position + pawn.Rotation.Opposite.FacingCell, 0.1f, $"{memoryValue.pathCost}", 60); 
                    }
#endif
                }

                if (__result != null)
                {
                    __result.collideWithPawns = true;
                    __result.expiryInterval = Rand.RangeInclusive(60, 180);
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

                if (memoryValue.blockingThing == null || (!pawn.HasReserved(blockingThing) && !pawn.CanReserve(blockingThing)))
                {
                    IntVec3 intVec = CellFinder.RandomClosewalkCellNear(blockingThing.Position, pawn.Map, 5, null);
                    if (intVec == pawn.Position)
                    {
                        job = JobMaker.MakeJob(JobDefOf.Wait, 20, true);
                    }
                    job = JobMaker.MakeJob(JobDefOf.Goto, intVec, 500, true);
                }
                else
                {
                    //if (memoryValue.blockingThing != null && distanceToTarget < 15)
                    //{
                    //using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, cellBeforeBlocker,
                    //        TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false, false, false), PathEndMode.Touch))
                    //{
                    //    if (pawnPath != PawnPath.NotFound && !pawnPath.nodes.Any(x => PawnUtility.AnyPawnBlockingPathAt(x, pawn, true, true, false)))
                    //    {
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
                            Log.Message($"{pawn} reserving {blockingThing} with {job}");
                            //pawn.ClearAllReservations();
                            pawn.Reserve(blockingThing, job);
                        }
                    }
                    
                    // }
                    //}
                    //}
                    //if (job == null || job.def == JobDefOf.AttackMelee)
                    //{
                    //    //if (!pawn.pather.MovedRecently(60) && Utilities.ThingBlocked(pawn, IntVec3.Invalid))
                    //    //{
                    //    //    job = Utilities.GetTrashNearbyWallJob(pawn, 1);
                    //    //}
                    //    if (job == null)
                    //    {
                    //        IntVec3 intVec = CellFinder.RandomClosewalkCellNear(cellBeforeBlocker, pawn.Map, 5, null);
                    //        if (intVec == pawn.Position)
                    //        {
                    //            job = JobMaker.MakeJob(JobDefOf.Wait, 20, true);
                    //        }
                    //        job = JobMaker.MakeJob(JobDefOf.Goto, intVec, 500, true);
                    //    }
                    //}
                }               

                return job;
            }
        }
    }
}
