using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using Unity.Jobs;
using static UnityEngine.GraphicsBuffer;
using Mono.Unix.Native;

namespace PogoAI.Patches
{
    internal class JobGiver_AIFightEnemy
    {
        [HarmonyPatch(typeof(RimWorld.JobGiver_AIFightEnemy), nameof(RimWorld.JobGiver_AIFightEnemy.TryGiveJob))]
        static class JobGiver_AIFightEnemy_TryGiveJob
        {
            static void Prefix(Pawn pawn, RimWorld.JobGiver_AIFightEnemy __instance)
            {
                __instance.needLOSToAcquireNonPawnTargets = true;
            }

            static void Postfix(Pawn pawn, ref Job __result)
            {
                if (__result != null && __result.targetA.Thing != null && __result.def == JobDefOf.AttackMelee)
                {
                    if (pawn.Map.avoidGrid.grid[pawn.Position] == 0 && pawn.Map.avoidGrid.grid[__result.targetA.Thing.Position] > 0)
                    {
                        JobGiver_AISapper.JobGiver_AISapper_TryGiveJob_Patch.Prefix(pawn, ref __result);
                    }
                    //Utilities.MaybeMoveOutTheWayJob(pawn, ref __result, __result.targetA.Thing);
                }

                //if (__result != null && __result.def == JobDefOf.AttackMelee && __result?.targetA.Thing?.Position != null)
                //{
                //    var target = __result.targetA.Thing.Position;
                //    if (pawn.Position.DistanceTo(target) > 3 && !pawn.CanReachImmediate(target, PathEndMode.Touch))
                //    {
                //        //if (Utilities.ThingBlocked(__result.targetA.Thing, IntVec3.Invalid, true))
                //        //{
                //        //    __result = null;
                //        //}
                //        //else
                //        //{
                //        //    using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, __result.targetA.Thing.Position,
                //        //                                TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false, false, false), PathEndMode.Touch))
                //        //    {
                //        //        if (pawnPath == PawnPath.NotFound || pawnPath.nodes.Any(x => PawnUtility.AnyPawnBlockingPathAt(x, pawn, true, true, false)))
                //        //        {
                //        //            __result = null;
                //        //        }
                //        //    }
                //        //}                        
                //    }
                //    else
                //    {
                //        Utilities.MaybeMoveOutTheWayJob(pawn, ref __result, __result.targetA.Thing);
                //    }
                //}
            }
        }
    }
}
