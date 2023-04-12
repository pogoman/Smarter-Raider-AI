using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using Verse;
using Verse.AI.Group;
using Unity.Jobs;
using static UnityEngine.GraphicsBuffer;

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
            public List<int> excludeList = new List<int>();

            public CachedPath(Pawn pawn, IAttackTarget targetThing, Thing blockingThing, IntVec3 cellBefore)
            {
                this.pawn = pawn;
                this.attackTarget = targetThing;
                this.blockingThing = blockingThing;
                this.cellBefore = cellBefore;
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
                    if (edifice != null && edifice.def.passability == Traversability.Impassable && !pawn.CanReserve(edifice))
                    {
                        return 10000;
                    }
                    else if (PawnUtility.AnyPawnBlockingPathAt(from, pawn, true, false, false))
                    {
                        return 10000;
                    }
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

                var potentials = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn).Where(x => !x.ThreatDisabled(pawn) && x.Thing.Faction == Faction.OfPlayer
                    && pawn.CanReach(x.Thing, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.PassAllDestroyableThings));

                if (pathCostCache.RemoveAll(x => x.attackTarget.ThreatDisabled(pawn) || x.attackTarget.Thing.Destroyed
                    || (x.blockingThing != null && x.blockingThing.Position.GetRegion(pawn.Map, RegionType.Set_Passable) != null)) > 0)
                {
                    findNewPaths = true;
                }

                var memoryValue = pathCostCache.OrderBy(x => pawn.Position.DistanceTo(x.cellBefore))
                    .FirstOrDefault(x => (x.blockingThing == null && !Utilities.PawnBlocked(x.attackTarget.Thing, IntVec3.Invalid)) || pawn.CanReserve(x.blockingThing));                
                IAttackTarget attackTarget = null;

                if (memoryValue != null)
                {
                    intVec = memoryValue.attackTarget.Thing.Position;
                    attackTarget = memoryValue.attackTarget;
                }

                if (findNewPaths)
                {   
                    if (!intVec.IsValid || attackTarget == null)
                    {
                        memoryValue = null;
                        attackTarget = potentials.OrderBy(x => pawn.Map.avoidGrid.grid[x.Thing.Position]).ThenBy(x => x.Thing.Position.DistanceTo(pawn.Position))
                            .FirstOrDefault(x => !Utilities.PawnBlocked(x.Thing, IntVec3.Invalid));
#if DEBUG
                        Log.Message($"new target: {attackTarget}");
#endif
                        if (attackTarget == null)
                        {
                            return false;
                        }
                        intVec = attackTarget.Thing.Position;
#if DEBUG
                        Find.CurrentMap.debugDrawer.FlashCell(pawn.Position, 0.7f, attackTarget.Thing.def.defName, 300);
#endif
                    }

                    if (memoryValue == null)
                    {
                        customTuning.custom = new CustomTuning(pawn);
                        using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, intVec,
                            TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings, false, true, false), PathEndMode.OnCell, customTuning))
                        {
                            IntVec3 cellBeforeBlocker = IntVec3.Invalid;
                            Thing blockingThing = pawnPath.FirstBlockingBuilding(out cellBeforeBlocker, pawn);
                            if (blockingThing == null && pawnPath.nodes.Count > 1)
                            {
                                cellBeforeBlocker = pawnPath.nodes[1];
                            }
                            else
                            {
#if DEBUG
                                Find.CurrentMap.debugDrawer.FlashCell(blockingThing.Position, 1f, "blocker", 300);
#endif
                            }
                            memoryValue = pathCostCache.FirstOrDefault(x => x.cellBefore == cellBeforeBlocker);
                            if (memoryValue == null)
                            {
                                memoryValue = new CachedPath(pawn, attackTarget, blockingThing, cellBeforeBlocker);
                                pathCostCache.Add(memoryValue);
                            }
                            else
                            {
                                findNewPaths = false;
                            }
                        }
                    }
                }
                else if (memoryValue == null)
                {
                    memoryValue = pathCostCache.First();
                }

#if DEBUG
                Find.CurrentMap.debugDrawer.FlashCell(pawn.Position, 0.8f, $"{memoryValue.attackTarget}", 60);
#endif
                __result = GetSapJob(pawn, memoryValue);
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
                
                if (memoryValue.blockingThing != null && memoryValue.blockingThing.def.mineable && !StatDefOf.MiningSpeed.Worker.IsDisabledFor(pawn))
                {
                    if (pawn.CanReserve(blockingThing))
                    {
                        job = JobMaker.MakeJob(JobDefOf.Mine, blockingThing);
                    }
                }
                else
                {
                    job = JobMaker.MakeJob(JobDefOf.AttackMelee, blockingThing);
                }
                if (job == null || job.def == JobDefOf.AttackMelee)
                {
                    if (distanceToTarget < 3 && Utilities.PawnBlocked(pawn, IntVec3.Invalid))
                    {
                        job = Utilities.GetTrashNearbyWallJob(pawn, 1);
                    }
                    if (job == null)
                    {
                        job = DigUtility.WaitNearJob(pawn, cellBeforeBlocker);
                    }
                }
                job.collideWithPawns = true;
                job.expiryInterval = Rand.RangeInclusive(60, 180);
                job.ignoreDesignations = true;
                job.checkOverrideOnExpire = true;
                job.expireRequiresEnemiesNearby = false;
                return job;
            }
        }
    }
}
