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
    internal class JobGiver_AIGotoNearestHostile
    {
        [HarmonyPatch(typeof(RimWorld.JobGiver_AIGotoNearestHostile), "TryGiveJob")]
        static class JobGiver_AIGotoNearestHostile_TryGiveJob_Patch
        {
            static bool Prefix(Pawn pawn, ref Job __result)
            {
                return pawn.Faction == Faction.OfInsects || !pawn.Map.IsPlayerHome || pawn.mindState?.duty?.def != DutyDefOf.AssaultColony || pawn.def.techLevel < Init.settings.minSmartTechLevel
                    || !Init.settings.everyRaidSaps;
            }
        }
    }
}
