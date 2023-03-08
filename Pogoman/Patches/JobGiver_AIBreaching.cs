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
    internal class JobGiver_AIBreaching
    {
        [HarmonyPatch(typeof(RimWorld.JobGiver_AIBreaching), "TryGiveJob")]
        private static class JobGiver_AIBreaching_TryGiveJob
        {
            public static bool Prefix(RimWorld.JobGiver_AIBreaching __instance, ref Job __result, Pawn pawn)
            {
                IntVec3 cell = pawn.mindState.duty.focus.Cell;
                if (cell.IsValid && (float)cell.DistanceToSquared(pawn.Position) < 25f && cell.GetRoom(pawn.Map) == pawn.GetRoom(RegionType.Set_All) && cell.WithinRegions(pawn.Position, pawn.Map, 9, TraverseMode.NoPassClosedDoors, RegionType.Set_Passable))
                {
                    pawn.GetLord().Notify_ReachedDutyLocation(pawn);
                    return false;
                }
                Verb verb = BreachingUtility.FindVerbToUseForBreaching(pawn);
                Log.Message($"{pawn} {verb}");
                if (verb == null)
                {
                    return false;
                }
                __instance.UpdateBreachingTarget(pawn, verb);
                BreachingTargetData breachingTarget = pawn.mindState.breachingTarget;
                Log.Message($"target {pawn} {breachingTarget}");
                if (breachingTarget == null)
                {
                    if (cell.IsValid && pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
                    {
                        Job job = JobMaker.MakeJob(JobDefOf.Goto, cell, 500, true);
                        BreachingUtility.FinalizeTrashJob(job);
                        __result = job;
                    }
                    return false;
                }
                else
                {
                    Log.Message($"firpos {pawn} {breachingTarget.firingPosition}");
                    if (!breachingTarget.firingPosition.IsValid)
                    {
                        return false;
                    }
                    Thing target = breachingTarget.target;
                    IntVec3 firingPosition = breachingTarget.firingPosition;
                    if (verb.IsMeleeAttack)
                    {
                        Job job2 = JobMaker.MakeJob(JobDefOf.AttackMelee, target, firingPosition);
                        job2.verbToUse = verb;
                        BreachingUtility.FinalizeTrashJob(job2);
                        __result = job2;
                        return false;
                    }
                    bool flag = firingPosition.Standable(pawn.Map) && pawn.Map.pawnDestinationReservationManager.CanReserve(firingPosition, pawn, false);
                    Job job3 = JobMaker.MakeJob(JobDefOf.UseVerbOnThing, target, flag ? firingPosition : IntVec3.Invalid);
                    job3.verbToUse = verb;
                    job3.preventFriendlyFire = true;
                    BreachingUtility.FinalizeTrashJob(job3);
                    __result = job3;
                    return false;
                }
            }
        }
    }
}
