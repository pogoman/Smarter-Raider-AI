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
    internal class JobGiver_Orders
    {

        [HarmonyPatch(typeof(Verse.AI.JobGiver_Orders), "TryGiveJob")]
        static class JobGiver_Orders_TryGiveJob_Patch
        {
            static void Postfix(Pawn pawn)
            {
                if (pawn.Drafted)
                {
                    pawn.Map.avoidGrid.gridDirty = true;
                }
            }
        }
    }
}
