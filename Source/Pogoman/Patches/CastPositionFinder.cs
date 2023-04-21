using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace PogoAI.Patches
{
    [HarmonyPatch(typeof(Verse.AI.CastPositionFinder), "TryFindCastPosition")]
    static class CastPositionFinder
    {
        public static void Postfix(CastPositionRequest newReq, ref bool __result)
        {
            var lordToil = RimWorld.BreachingUtility.LordToilOf(newReq.caster);
            if (!__result && (lordToil?.useAvoidGrid ?? false))
            {
                lordToil.useAvoidGrid = false;
                lordToil.Data.Reset();
#if DEBUG
                Log.Message($"Couldnt find cast position so disabling avoid grid for breaching");
#endif
            }
        }
    }
}
