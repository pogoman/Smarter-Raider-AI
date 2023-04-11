using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using Verse;
using Verse.AI.Group;
using Unity.Jobs;

namespace PogoAI.Patches
{
    public static class JobGiver_AISapper
    {
        const int minPathLengthToCache = 0;

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
            public CustomTuning()
            {
                excludeCells = new List<IntVec3>();
            }

            public int CostOffset(IntVec3 from, IntVec3 to)
            {                
                return excludeCells.Contains(from) ? 10000 : 0;
            }

            public List<IntVec3> excludeCells;

        }

        public static PathFinderCostTuning customTuning = new PathFinderCostTuning()
        {
            costOffLordWalkGrid = 0,
            costBlockedWallBase = 0,
            costBlockedWallExtraPerHitPoint = 5,
            costBlockedDoor = 0,
            costBlockedDoorPerHitPoint = 5,
            costBlockedWallExtraForNaturalWalls = 0,
            custom = new CustomTuning()
        };

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
#if DEBUG
                Find.CurrentMap.debugDrawer.FlashCell(pawn.Position, 0.8f, $"Sap", 60);
#endif
                IntVec3 intVec = pawn.mindState.duty.focus.Cell;
                if (intVec.IsValid && (float)intVec.DistanceToSquared(pawn.Position) < 100f && intVec.GetRoom(pawn.Map) == pawn.GetRoom(RegionType.Set_All) 
                    && intVec.WithinRegions(pawn.Position, pawn.Map, 9, TraverseMode.NoPassClosedDoors, RegionType.Set_Passable))
                {
                    pawn.GetLord().Notify_ReachedDutyLocation(pawn);
                    return false;
                }

                var potentials = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn).Where(x => !x.ThreatDisabled(pawn) && x.Thing.Faction == Faction.OfPlayer
                    && pawn.CanReach(x.Thing, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.PassAllDestroyableThings));

                pathCostCache.RemoveAll(x => x.attackTarget.ThreatDisabled(pawn) || (x.blockingThing != null && x.blockingThing.Position.GetRegion(pawn.Map, RegionType.Set_Passable) != null));
                CachedPath memoryValue = pathCostCache.FirstOrDefault();

                IAttackTarget attackTarget = null;
                if (memoryValue != null)
                {
                    intVec = memoryValue.attackTarget.Thing.Position;
                    attackTarget = memoryValue.attackTarget;
                }

                if (!intVec.IsValid || attackTarget == null)
                {
                    memoryValue = null;
                    attackTarget = potentials.OrderBy(x => pawn.Map.avoidGrid.grid[x.Thing.Position]).ThenBy(x => x.Thing.Position.DistanceTo(pawn.Position)).FirstOrDefault();
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
                    using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, intVec,
                        TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings, false, true, false), PathEndMode.OnCell, customTuning))
                    {
                        IntVec3 cellBeforeBlocker = IntVec3.Invalid;
                        IntVec3 cellAfterBlocker = IntVec3.Invalid;
                        Thing blockingThing = FirstBlockingBuilding(pawnPath, out cellBeforeBlocker, out cellAfterBlocker, pawn);
                        IntVec3 cellBeforeTarget = IntVec3.Invalid;
                        memoryValue = new CachedPath(pawn, attackTarget, blockingThing, cellBeforeBlocker, cellAfterBlocker);
                        pathCostCache.Add(memoryValue);
                        if (blockingThing != null)
                        {
                            ((CustomTuning)customTuning.custom).excludeCells.Add(cellBeforeBlocker);
                        }
                    }
                }

                __result = GetSapJob(pawn, memoryValue);
                return false;
            }            

            private static Job GetSapJob(Pawn pawn, CachedPath memoryValue)
            {
                var cellBeforeBlocker = memoryValue.cellBefore;
                var blockingThing = memoryValue.blockingThing;
                if (memoryValue.blockingThing == null)
                {
                    cellBeforeBlocker = memoryValue.attackTarget.Thing.Position;
                    blockingThing = memoryValue.attackTarget.Thing;
                }
                Job job = null;                
                
                if (pawn.CanReserve(blockingThing))
                {
                    if (memoryValue.blockingThing != null && memoryValue.blockingThing.def.mineable && !StatDefOf.MiningSpeed.Worker.IsDisabledFor(pawn))
                    {
                        job = JobMaker.MakeJob(JobDefOf.Mine, blockingThing);
                    }
                    else
                    {
                        job = JobMaker.MakeJob(JobDefOf.AttackMelee, blockingThing);
                    }
                    job.expireRequiresEnemiesNearby = false;
                }
                else
                {
                    Job trashJob = null;                     
                    if (pawn.Position.DistanceTo(cellBeforeBlocker) < 15)
                    {
                        trashJob = Utilities.GetTrashNearbyWallJob(pawn, 10);
                    }
                    if (trashJob != null)
                    {
                        job = trashJob;
                    }
                    else
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

            private static Thing FirstBlockingBuilding(PawnPath path, out IntVec3 cellBefore, out IntVec3 cellAfter, Pawn pawn)
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
#if DEBUG
                        Find.CurrentMap.debugDrawer.FlashCell(nodesReversed[i], 0.5f, "block", 60);
#endif
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
        }
    }
}
