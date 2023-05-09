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

    [HarmonyPatch(typeof(RimWorld.JobGiver_AIBreaching), "UpdateBreachingTarget")]
    static class JobGiver_AIBreaching_UpdateBreachingTarget
    {
        public static void Prefix(Pawn pawn)
        {
            var breachingTargetData = pawn.mindState.breachingTarget;
            if (breachingTargetData != null && breachingTargetData.target == null)
            {
                pawn.mindState.breachingTarget = null;
                var lordToil_AssaultColonyBreaching = RimWorld.BreachingUtility.LordToilOf(pawn);
                lordToil_AssaultColonyBreaching.Data.currentTarget = null;
                if (!BreachingUtility.breachMineables)
                {
                    BreachingUtility.breachMineables = true;
#if DEBUG
                    Log.Message("Target is mineable, disabling breaching filter...");
#endif
                }
            }
        }
    }
}
