using HarmonyLib;
using Verse;

namespace PogoAI.Patches
{
    [HarmonyPatch(typeof(Verse.AI.Group.Lord), "LordTick")]
    static class Lord_LordTick
    {
        static void Postfix(Verse.AI.Group.Lord __instance)
        {
            //The 1.6 pathfinder only reads AvoidGrid.Grid when rebuilding its cached cost
            //grids, so lazily waiting on gridDirty leaves the avoid grid stale. Regenerate
            //proactively whenever hostiles are around; the prefix throttles the real work.
            var comp = PogoMapComponent.For(__instance.Map);
            if (comp != null && (comp.lastAvoidGridUpdateTicks == 0
                || Find.TickManager.TicksGame - comp.lastAvoidGridUpdateTicks >= AvoidGrid_Regenerate.UpdateIntervalTicks))
            {
                __instance.Map.avoidGrid.Regenerate();
            }
        }
    }
}
