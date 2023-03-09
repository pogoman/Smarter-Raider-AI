using HarmonyLib;
using System.Collections.Generic;
using Verse;

namespace PogoAI.Patches.CE
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
                List<Building> allBuildingsColonist = __instance.map.listerBuildings.allBuildingsColonist;
                for (int i = 0; i < allBuildingsColonist.Count; i++)
                {
                    if (allBuildingsColonist[i].def.building.ai_combatDangerous)
                    {
                        var building = allBuildingsColonist[i];
                        CompEquippable equip = (CompEquippable)building.GetType().GetProperty("GunCompEq").GetValue(building, null);
                        Log.Message($"{equip}");
                        PrintAvoidGridAroundTurret(__instance, building, equip);
                    }
                }
                __instance.ExpandAvoidGridIntoEdifices();

                return false;
            }

            static void PrintAvoidGridAroundTurret(Verse.AI.AvoidGrid __instance, Building tur, CompEquippable equip)
            {
                float range = equip.PrimaryVerb.verbProps.range;
                float num = equip.PrimaryVerb.verbProps.EffectiveMinRange(true);
                int num2 = GenRadial.NumCellsInRadius(range + 4f);
                for (int i = num < 1f ? 0 : GenRadial.NumCellsInRadius(num); i < num2; i++)
                {
                    IntVec3 intVec = tur.Position + GenRadial.RadialPattern[i];
                    if (intVec.InBounds(tur.Map) && intVec.WalkableByNormal(tur.Map) && GenSight.LineOfSight(intVec, tur.Position, tur.Map, true, null, 0, 0))
                    {
                        __instance.IncrementAvoidGrid(intVec, 45);
                    }
                }
            }
        }
    }
}
