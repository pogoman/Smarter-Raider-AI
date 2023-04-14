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
            if (__instance.pawn.Position.IsValid && __instance.pawn.Faction.HostileTo(Faction.OfPlayer) && __instance.pawn.Faction != Faction.OfInsects)
            {
                AvoidGrid_Regenerate.PrintAvoidGridAroundPos(__instance.pawn.Map.avoidGrid, __instance.pawn.Map, __instance.pawn.Position, 2, 1000);
                JobGiver_AISapper.pathCostCache.RemoveAll(x => x.blockingThing == null && x.cellBefore.DistanceTo(__instance.pawn.Position) < 5);
                //var nearbyRaiders = __instance.pawn.Map.mapPawns.PawnsInFaction(__instance.pawn.Faction).Where(x => x.Position.DistanceTo(__instance.pawn.Position) < 5);
                //foreach (var raider in nearbyRaiders)
                //{
                //    raider.jobs.StopAll();
                //    raider.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait, 20, true), JobCondition.InterruptForced, null, false, true, null, null, false, false, null, false, true);
                //}
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
