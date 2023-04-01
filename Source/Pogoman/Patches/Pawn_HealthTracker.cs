using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Jobs;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Noise;

namespace PogoAI.Patches
{
    internal class Pawn_HealthTracker
    {
        static void GenerateAvoidGrid(Verse.Pawn_HealthTracker __instance)
        {
            if (__instance.pawn.Faction.HostileTo(Faction.OfPlayer) 
                && GenClosest.ClosestThing_Global(__instance.pawn.Position, __instance.pawn.Map.listerBuildings.allBuildingsColonist, 10f) != null)
            {
                AvoidGrid.AvoidGrid_Regenerate.PrintAvoidGridAroundPos(__instance.pawn.Map.avoidGrid, __instance.pawn.Map, __instance.pawn.Position, 1, 1000);
                var nearbyRaiders = __instance.pawn.Map.mapPawns.PawnsInFaction(__instance.pawn.Faction).Where(x => x.Position.DistanceTo(__instance.pawn.Position) <= 5);
                foreach (var raider in nearbyRaiders)
                {
                    raider.jobs.StopAll();
                    raider.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait, 20, true), JobCondition.InterruptForced, null, false, true, null, null, false, false, null, false, true);
                }
            }
        }

        [HarmonyPatch(typeof(Verse.Pawn_HealthTracker), "SetDead")]
        static class Pawn_HealthTracker_SetDead
        {     
            static void Postfix(Verse.Pawn_HealthTracker __instance)
            {
                GenerateAvoidGrid(__instance);
            }
        }

        [HarmonyPatch(typeof(Verse.Pawn_HealthTracker), "MakeDowned")]
        static class Pawn_HealthTracker_MakeDowned
        {
            static void Postfix(Verse.Pawn_HealthTracker __instance)
            {
                GenerateAvoidGrid(__instance);
            }
        }
    }
}
