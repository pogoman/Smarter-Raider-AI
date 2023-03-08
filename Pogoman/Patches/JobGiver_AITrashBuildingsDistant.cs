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
    internal class JobGiver_AITrashBuildingsDistant
    {
        [HarmonyPatch(typeof(RimWorld.JobGiver_AITrashBuildingsDistant), "TryGiveJob")]
        private static class JobGiver_AITrashBuildingsDistant_TryGiveJob_Patch
        {
            public static bool Prefix(Pawn pawn, ref Job __result)
            {
                return false;
            }
        }
    }
}
