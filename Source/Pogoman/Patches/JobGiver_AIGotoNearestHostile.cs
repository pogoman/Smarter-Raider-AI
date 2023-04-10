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
            static void Postfix(Pawn pawn, ref Job __result)
            {
                if (__result?.def == JobDefOf.Goto && Utilities.GetNearbyThing(pawn, ThingDefOf.TrapSpike, 2) != null
                    && pawn.Map.avoidGrid.grid[pawn.Position] != 0)
                {
#if DEBUG
                    Find.CurrentMap.debugDrawer.FlashCell(pawn.Position, 1f, $"TRAP", 120);
#endif
                    var tryTrashJob = Utilities.GetTrashNearbyWallJob(pawn, 1);
                    if (tryTrashJob != null)
                    {
                        __result = tryTrashJob;
                    }
                }
            }
        }


    }
}
