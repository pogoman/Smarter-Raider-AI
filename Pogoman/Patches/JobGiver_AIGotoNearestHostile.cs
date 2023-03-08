using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using RimWorld;
using Verse.AI.Group;

namespace PogoAI.Patches
{
    internal class JobGiver_AIGotoNearestHostile
    {
        [HarmonyPatch(typeof(RimWorld.JobGiver_AIGotoNearestHostile), "TryGiveJob")]
        private static class JobGiver_AIGotoNearestHostile_TryGiveJob_Patch
        {
            public static bool Prefix(Pawn pawn, ref Job __result)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(JobGiver_AITrashColonyClose), nameof(JobGiver_AITrashColonyClose.TryGiveJob))]
        private static class JobGiver_AITrashColonyClose_TryGiveJob_Patch
        {
            public static void Prefix(Pawn pawn, ref Job __result)
            {
                //Log.Message($"JobGiver_AITrashColonyClose");
            }
        }

        [HarmonyPatch(typeof(JobGiver_AIFightEnemy), nameof(JobGiver_AIFightEnemy.TryGiveJob))]
        private static class JobGiver_AIFightEnemy_TryGiveJob_Patch
        {
            public static void Prefix(Pawn pawn, JobGiver_AIFightEnemy __instance)
            {
                //Log.Message($"JobGiver_AIFightEnemy {pawn.mindState.duty.def}");
                __instance.needLOSToAcquireNonPawnTargets = true;
            }
        }
    }
}
