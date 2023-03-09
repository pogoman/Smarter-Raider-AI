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
    internal class JobGiverTweaks
    {
        [HarmonyPatch(typeof(JobGiver_AIGotoNearestHostile), "TryGiveJob")]
        static class JobGiver_AIGotoNearestHostile_TryGiveJob_Patch
        {
            static bool Prefix()
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(JobGiver_AIFightEnemy), nameof(JobGiver_AIFightEnemy.TryGiveJob))]
        static class JobGiver_AIFightEnemy_TryGiveJob_Patch
        {
            static void Prefix(Pawn pawn, JobGiver_AIFightEnemy __instance)
            {
                __instance.needLOSToAcquireNonPawnTargets = true;
            }
        }

        [HarmonyPatch(typeof(RimWorld.JobGiver_AITrashBuildingsDistant), "TryGiveJob")]
        static class JobGiver_AITrashBuildingsDistant_TryGiveJob_Patch
        {
            static bool Prefix()
            {
                return false;
            }
        }
    }
}
