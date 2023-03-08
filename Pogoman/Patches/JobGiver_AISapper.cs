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

namespace PogoAI.Patches
{
    internal class JobGiver_AISapper
    {
        [HarmonyPatch(typeof(RimWorld.JobGiver_AISapper), "TryGiveJob")]
        private static class JobGiver_AISapper_TryGiveJob_Patch
        {
            public static bool Prefix(Pawn pawn, ref Job __result, RimWorld.JobGiver_AISapper __instance)
            {
                IntVec3 intVec = pawn.mindState.duty.focus.Cell;
                if (intVec.IsValid && (float)intVec.DistanceToSquared(pawn.Position) < 100f && intVec.GetRoom(pawn.Map) == pawn.GetRoom(RegionType.Set_All) && intVec.WithinRegions(pawn.Position, pawn.Map, 9, TraverseMode.NoPassClosedDoors, RegionType.Set_Passable))
                {
                    pawn.GetLord().Notify_ReachedDutyLocation(pawn);
                    return false;
                }
                if (!intVec.IsValid)
                {
                    Thing attackTarget;
                    if (!pawn.Map.listerThings.AllThings.Where(x => x.Faction == Faction.OfPlayer
                        && pawn.CanReach(x, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.PassAllDestroyableThings))
                          .TryRandomElement(out attackTarget))
                    {
                        return false;
                    }

                    intVec = attackTarget.Position;
                }
                if (!pawn.CanReach(intVec, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.PassAllDestroyableThings))
                {
                    return false;
                }
                
                var customTuning = new PathFinderCostTuning() { custom = new InEnemyLosPathFinderTuning(pawn.Map, pawn.GetLord()) };
                using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, intVec,
                    TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings, false, false, false), PathEndMode.OnCell, customTuning))
                {
                    IntVec3 cellBeforeBlocker;
                    Thing thing = pawnPath.FirstBlockingBuilding(out cellBeforeBlocker, pawn);
                    //Log.Message($"tuned: start {pawnPath.FirstNode} finish {pawnPath.LastNode} cost {pawnPath.TotalCost} thing {thing}");
                    if (thing != null)
                    {
                        Job job = DigUtility.PassBlockerJob(pawn, thing, cellBeforeBlocker, __instance.canMineMineables, __instance.canMineNonMineables);
                        if (job != null)
                        {
                            __result = job;
                            return false;
                        }
                    }
                }
                Log.Message($"{pawn} No sap requied goto instead");
                return false;
            }
        }
    }
}
