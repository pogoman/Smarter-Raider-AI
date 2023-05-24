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
#if DEBUG
                Log.Message($"Couldnt find breach job so disabling avoid grid for breaching");
#endif
            }
            else if (__result != null)
            {
                __result.expiryInterval = Rand.RangeInclusive(120, 240);
            }
        }
    }
}
