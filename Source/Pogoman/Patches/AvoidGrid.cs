using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Unity.Baselib.LowLevel;
using Verse;

namespace PogoAI.Patches
{
    internal class AvoidGrid
    {
        [HarmonyPatch(typeof(Verse.AI.AvoidGrid), "Regenerate")]
        static class AvoidGrid_Regenerate
        {
            static bool Prefix(Verse.AI.AvoidGrid __instance)
            {
                __instance.gridDirty = false;
                __instance.grid.Clear(0);
                var draftedColonists = __instance.map.PlayerPawnsForStoryteller.Where(x => x.Drafted);
                foreach (var pawn in draftedColonists)
                {
                    PrintAvoidGridAroundThing(__instance, pawn.Map, pawn.Position, pawn.equipment.PrimaryEq.PrimaryVerb);
                }
                List<Building> allBuildingsColonist = __instance.map.listerBuildings.allBuildingsColonist;
                for (int i = 0; i < allBuildingsColonist.Count; i++)
                {
                    if (allBuildingsColonist[i].def.building.ai_combatDangerous)
                    {
                        if (Init.combatExtended)
                        {
                            var building = allBuildingsColonist[i];
                            CompEquippable equip = (CompEquippable)building.GetType().GetProperty("GunCompEq").GetValue(building, null);
                            PrintAvoidGridAroundThing(__instance, building.Map, building.Position, equip.PrimaryVerb);
                        }
                        else
                        {
                            Building_TurretGun building_TurretGun = allBuildingsColonist[i] as Building_TurretGun;
                            if (building_TurretGun != null)
                            {
                                __instance.PrintAvoidGridAroundTurret(building_TurretGun);
                            }
                        }
                    }
                }
                __instance.ExpandAvoidGridIntoEdifices();
                return false;
            }

            static void PrintAvoidGridAroundThing(Verse.AI.AvoidGrid __instance, Map map, IntVec3 pos, Verb verb)
            {
                float range = verb.verbProps.range;
                if (verb.IsMeleeAttack)
                {
                    range = 2;
                }
                float num = verb.verbProps.EffectiveMinRange(true);
                int num2 = GenRadial.NumCellsInRadius(range);
                for (int i = num < 1f ? 0 : GenRadial.NumCellsInRadius(num); i < num2; i++)
                {
                    IntVec3 intVec = pos + GenRadial.RadialPattern[i];
                    if (intVec.InBounds(map) && intVec.WalkableByNormal(map) && GenSight.LineOfSight(intVec, pos, map, true, null, 0, 0))
                    {
                        __instance.IncrementAvoidGrid(intVec, 45);
                    }
                }
            }
        }
    }
}
