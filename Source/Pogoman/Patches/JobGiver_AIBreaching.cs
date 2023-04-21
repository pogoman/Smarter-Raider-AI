using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace PogoAI.Patches
{
    [HarmonyPatch(typeof(RimWorld.JobGiver_AIBreaching), "TryGiveJob")]
    static class JobGiver_AIBreaching_TryGiveJob
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            var lordToil = RimWorld.BreachingUtility.LordToilOf(pawn);
            if (__result == null && (lordToil?.useAvoidGrid ?? false))
            {
                lordToil.Data.Reset();
                lordToil.useAvoidGrid = false;
            }
        }
    }
}
