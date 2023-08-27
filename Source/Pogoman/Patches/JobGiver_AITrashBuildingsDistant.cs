using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using static PogoAI.Patches.JobGiver_AISapper;

namespace PogoAI.Patches
{
    [HarmonyPatch(typeof(RimWorld.JobGiver_AITrashBuildingsDistant), "TryGiveJob")]
    static class JobGiver_AITrashBuildingsDistant_TryGiveJob_Patch
    {
        static bool Prefix(Pawn pawn, ref Job __result)
        {
            if (pawn.mindState?.duty?.def != DutyDefOf.AssaultColony || pawn.Faction.def.techLevel < Init.settings.minSmartTechLevel
                || pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn).Count(x => !x.ThreatDisabled(pawn) && !x.Thing.Destroyed && x.Thing.Faction == Faction.OfPlayer) == 0)
            {
                return true;
            }
            JobGiver_AISapper_TryGiveJob_Patch.Prefix(pawn, ref __result);
            return false;
        }
    }
}
