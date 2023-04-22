using HarmonyLib;
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
    [HarmonyPatch(typeof(Verse.AI.CastPositionFinder), "TryFindCastPosition")]
    static class CastPositionFinder
    {
        public static void Postfix(CastPositionRequest newReq, ref bool __result)
        {
            var lord = newReq.caster?.GetLord();
            if (lord != null && lord.CurLordToil is RimWorld.LordToil_AssaultColonyBreaching)
            {
                var lordToil = lord.CurLordToil as RimWorld.LordToil_AssaultColonyBreaching;
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
}
