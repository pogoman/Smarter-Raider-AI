using HarmonyLib;
using RimWorld;
using Verse;

namespace PogoAI.Patches
{

    [HarmonyPatch(typeof(Verse.Pawn_HealthTracker), "SetDead")]
    static class Pawn_HealthTracker_SetDead
    {
        static void Postfix(Verse.Pawn_HealthTracker __instance)
        {
            GridOnDeath(__instance);
        }

        public static void GridOnDeath(Pawn_HealthTracker __instance)
        {
            var pawn = __instance.pawn;
            if (pawn == null || pawn.Map == null || !pawn.Position.IsValid
                || !(pawn.Faction?.HostileTo(Faction.OfPlayer) ?? false) || pawn.Faction == Faction.OfInsects)
            {
                return;
            }
            var avoidGrid = pawn.Map.avoidGrid;
            if (avoidGrid?.Grid != null)
            {
                var clearPaths = avoidGrid.Grid[pawn.Map.cellIndices.CellToIndex(pawn.Position)] == 0;
                AvoidGrid_Regenerate.PrintAvoidGridAroundPos(avoidGrid, pawn.Map, pawn.Position, 1, 1000);
                if (pawn.mindState?.enemyTarget != null)
                {
                    AvoidGrid_Regenerate.PrintAvoidGridAroundPos(avoidGrid, pawn.Map, pawn.mindState.enemyTarget.Position, 1, 1000);
                }
                if (clearPaths && Utilities.GetNearestThingDesignationDef(pawn, DesignationCategoryDefOf.Production, 1) != null)
                {
#if DEBUG
                    Log.Message($"{pawn} died in near base, clearing cache...");
#endif
                    PogoMapComponent.For(pawn.Map)?.ResetSapperState();
                }
            }
        }
    }

    [HarmonyPatch(typeof(Verse.Pawn_HealthTracker), "MakeDowned")]
    static class Pawn_HealthTracker_MakeDowned
    {
        static void Postfix(Pawn_HealthTracker __instance)
        {
            Pawn_HealthTracker_SetDead.GridOnDeath(__instance);
        }
    }

}
