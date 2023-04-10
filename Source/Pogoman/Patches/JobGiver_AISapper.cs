using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using Verse;
using Verse.AI.Group;

namespace PogoAI.Patches
{
    public static class JobGiver_AISapper
    {
        const int minPathLengthToCache = 0;

        public class CachedPath
        {
            public Pawn pawn;
            public Thing targetThing;
            public IntVec3 cellBefore;
            public IntVec3 cellAfter;
            public List<int> excludeList = new List<int>();
            public bool notBlocked = false;

            public CachedPath(Pawn pawn, Thing targetThing, IntVec3 cellBefore, IntVec3 cellAfter, bool notBlocked)
            {
                this.pawn = pawn;
                this.targetThing = targetThing;
                this.cellBefore = cellBefore;
                this.cellAfter = cellAfter;
                this.notBlocked = notBlocked;
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
                IntVec3 intVec = pawn.mindState.duty.focus.Cell;
                if (intVec.IsValid && (float)intVec.DistanceToSquared(pawn.Position) < 100f && intVec.GetRoom(pawn.Map) == pawn.GetRoom(RegionType.Set_All) 
                    && intVec.WithinRegions(pawn.Position, pawn.Map, 9, TraverseMode.NoPassClosedDoors, RegionType.Set_Passable))
                {
                    pawn.GetLord().Notify_ReachedDutyLocation(pawn);
                    return false;
                }

                CachedPath memoryValue = pathCostCache.FirstOrDefault(x => x.targetThing?.HitPoints > 0 && !x.notBlocked 
                    && !x.excludeList.Contains(pawn.GetHashCode()) && x.targetThing?.Position.GetRegion(pawn.Map, RegionType.Set_Passable) == null
                    && !pawn.Map.reachability.CanReach(x.cellBefore, x.cellAfter, PathEndMode.Touch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn)));

                Thing attackTarget = null;
                if (memoryValue != null)
                {
                    intVec = memoryValue.targetThing.Position;
                    attackTarget = memoryValue.targetThing;
                }

                if (attackTarget == null)
                {
                    //var targets = pawn.Map.attackTargetsCache.TargetsHostileToFaction(pawn.Faction).Where(x => !x.ThreatDisabled(pawn)
                    //    && x.Thing.Faction == Faction.OfPlayer && !blockedPositions.Any(y => x.Thing.Position == y)).Select(x => x.Thing).ToList();

                    //If we enter here we want a new target thing where path cost never been calced before.
                    attackTarget = pawn.Map.listerBuildings.allBuildingsColonist.OrderBy(x => x.Position.DistanceTo(pawn.Position)).Where(x => x.def.designationCategory != DesignationCategoryDefOf.Structure
                        && x.def.designationCategory != DesignationCategoryDefOf.Security && !x.def.IsFrame && x.HitPoints > 0 && x.def.altitudeLayer != AltitudeLayer.Conduits
                       && !pathCostCache.Any(y => x == y.targetThing)).FirstOrDefault();

                    if (attackTarget == null)
                    {
                        return false;
                    }
                    intVec = attackTarget.Position;
                }


                #if DEBUG
                    Find.CurrentMap.debugDrawer.FlashCell(pawn.Position, 0.7f, attackTarget.def.defName, 300);
                #endif

                if (memoryValue == null)
                {                   
                    using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, intVec,
                        TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings, false, false, false), PathEndMode.OnCell, customTuning))
                    {
                        IntVec3 cellBeforeBlocker = IntVec3.Invalid;
                        IntVec3 cellAfterBlocker = IntVec3.Invalid;
                        Thing thing = FirstBlockingBuilding(pawnPath, out cellBeforeBlocker, out cellAfterBlocker, pawn);
                        IntVec3 cellBeforeTarget = IntVec3.Invalid;
                        var notBlocked = false;
                        //var meleePathBlocked = false;
                        if (thing == null && pawnPath.nodes.Count > 1)
                        {
                            cellBeforeTarget = pawnPath.nodes[1];
                            thing = attackTarget;
                            notBlocked = true;
                            #if DEBUG
                                Find.CurrentMap.debugDrawer.FlashCell(attackTarget.Position, 1f, "noblocker", 300);
                            #endif
                        }
                        if (pawnPath.nodes?.Count >= minPathLengthToCache)
                        {
                            pathCostCache.Add(new CachedPath(pawn, thing, cellBeforeBlocker, cellAfterBlocker, notBlocked));
                            ((CustomTuning)customTuning.custom).excludeCells.Add(cellBeforeBlocker);
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
                        if (job.def == JobDefOf.Wait || (job.def == JobDefOf.Goto && job.targetA.Cell.DistanceTo(pawn.Position) < 15))
                        {
                            job = Utilities.GetTrashNearbyWallJob(pawn, 10);
                            if (job == null)
                            {
                                #if DEBUG
                                    Find.CurrentMap.debugDrawer.FlashCell(pawn.Position, 0f, $"SNT", 120);
                                #endif
                                pathCostCache.Where(x => x.targetThing == thing).ToList().ForEach(x => x.excludeList.Add(pawn.GetHashCode()));
                            }
                        }
                        else
                        {
                            job.expireRequiresEnemiesNearby = true;
                        }
                    }
                }
                return job;
            }
        }
    }
}
