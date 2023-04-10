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
    public static class JobGiver_AITrashBuildingsDistant
    {
        [HarmonyPatch(typeof(RimWorld.JobGiver_AITrashBuildingsDistant), "TryGiveJob")]
        static class JobGiver_AITrashBuildingsDistant_TryGiveJob_Patch
        {
            static bool Prefix(ref RimWorld.JobGiver_AITrashBuildingsDistant __instance, Pawn pawn, ref Job __result)
            {
                return false;                
            }
        }
    }
}
