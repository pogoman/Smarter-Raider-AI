using HarmonyLib;
using RimWorld;
using Verse.AI;
using Verse;
using System.Linq;

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
            if (__instance.pawn != null && __instance.pawn.Position.IsValid && __instance.pawn.Faction.HostileTo(Faction.OfPlayer) && __instance.pawn.Faction != Faction.OfInsects)
            {
                AvoidGrid_Regenerate.PrintAvoidGridAroundPos(__instance.pawn.Map.avoidGrid, __instance.pawn.Map, __instance.pawn.Position, 1, 1000);
                JobGiver_AISapper.pathCostCache.RemoveAll(x => x.blockingThing == null && x.cellBefore.DistanceTo(__instance.pawn.Position) < 5);
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
