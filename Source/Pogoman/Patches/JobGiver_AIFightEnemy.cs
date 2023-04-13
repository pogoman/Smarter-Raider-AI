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
                if (__result != null && __result.def == JobDefOf.AttackMelee && __result?.targetA.Thing?.Position != null)
                {
                    var target = __result.targetA.Thing.Position;
                    if (pawn.Position.DistanceTo(target) > 3 && !pawn.CanReachImmediate(target, PathEndMode.Touch) 
                        && (Utilities.ThingBlocked(__result.targetA.Thing, IntVec3.Invalid) || Utilities.ThingBlocked(pawn, pawn.Position + pawn.Rotation.Opposite.FacingCell))
                        || !pawn.pather.MovedRecently(120))
                    {
                        __result = null;
                    }
                    else
                    {
                        Utilities.MaybeMoveOutTheWayJob(pawn, ref __result, __result.targetA.Thing);
                    }
                }
            }
        }
    }
}
