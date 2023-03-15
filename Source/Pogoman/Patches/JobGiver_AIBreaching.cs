using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace PogoAI.Patches
{
    internal class JobGiver_AIBreaching
    {

        [HarmonyPatch(typeof(RimWorld.JobGiver_AIBreaching), "TryGiveJob")]
        static class JobGiver_AIBreaching_TryGiveJob_Patch
        {
            static void Postfix(Pawn pawn)
            {
                if ((pawn.mindState.breachingTarget == null || !pawn.mindState.breachingTarget.firingPosition.IsValid) 
                    && pawn.CurJobDef == JobDefOf.GotoWander)
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
                }
            }
        }

        [HarmonyPatch(typeof(RimWorld.JobGiver_AIBreaching), "UpdateBreachingTarget")]
        static class JobGiver_AIBreaching_UpdateBreachingTarget
        {
            static bool Prefix(Pawn pawn)
            {
                if (pawn.mindState.breachingTarget != null && pawn.mindState.breachingTarget.target == null)
                {
                    pawn.mindState.breachingTarget.target = new Thing() { mapIndexOrState = -2 };
                }
                return true;
            }
        }
    }
}
