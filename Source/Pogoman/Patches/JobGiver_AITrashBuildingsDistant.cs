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
    [HarmonyPatch(typeof(RimWorld.JobGiver_AITrashBuildingsDistant), "TryGiveJob")]
    static class JobGiver_AITrashBuildingsDistant_TryGiveJob_Patch
    {
        static bool Prefix()
        {
            return false;                
        }
    }
}
