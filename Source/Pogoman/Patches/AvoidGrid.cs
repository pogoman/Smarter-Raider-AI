using HarmonyLib;
using Mono.Unix.Native;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using Unity.Baselib.LowLevel;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PogoAI.Patches
{
    internal class AvoidGrid
    {

        [HarmonyPatch(typeof(Verse.AI.AvoidGrid), "Regenerate")]
        static class AvoidGrid_Regenerate
        {
            static Verse.AI.AvoidGrid instance;
            static int counter = 0;

            static bool Prefix(Verse.AI.AvoidGrid __instance)
            {
                instance = __instance;
                __instance.gridDirty = false;
                __instance.grid.Clear(0);
                counter = 0;
                var draftedColonists = __instance.map.PlayerPawnsForStoryteller.Where(x => 
                    x.Drafted && x.equipment?.PrimaryEq != null && x.CurJobDef == JobDefOf.Wait_Combat);
                foreach (var pawn in draftedColonists)
                {                   
                    PrintAvoidGridAroundThing(__instance, pawn.Map, pawn.Position, pawn.equipment.PrimaryEq.PrimaryVerb, true);
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
                                PrintAvoidGridAroundThing(__instance, building_TurretGun.Map,
                                    building_TurretGun.Position, building_TurretGun.GunCompEq.PrimaryVerb);
                            }
                        }
                    }
                }
                __instance.ExpandAvoidGridIntoEdifices();
                //Log.Message($"Count: {counter}");
                return false;
            }

            static void PrintAvoidGridAroundThing(Verse.AI.AvoidGrid __instance, Map map, IntVec3 pos, Verb verb, bool isPawn = false)
            {
                if (verb.Caster.def.defName == "Turret_RocketswarmLauncher") {
                    return;
                }
                float range = verb.verbProps.range;
                if (verb.IsMeleeAttack)
                {
                    range = 2;
                }
                float num = verb.verbProps.EffectiveMinRange(true);
                int num2 = GenRadial.NumCellsInRadius(range);
                for (int i = num2; i > (num < 1f ? 0 : GenRadial.NumCellsInRadius(num)); i--)
                {
                    IntVec3 intVec = pos + GenRadial.RadialPattern[i];
                    LocalTargetInfo targ = new LocalTargetInfo(intVec);
                    ShootLine shootLine;
                    if (intVec.InBounds(map) && intVec.WalkableByNormal(map)
                        && __instance.grid[intVec] == 0
                        && verb.TryFindShootLineFromTo(pos, targ, out shootLine))
                    {
                        counter++;
                        GenSight.PointsOnLineOfSight(intVec, pos, incrementAvoidGrid);
                    }
                }
            }

            private static Action<IntVec3> incrementAvoidGrid = new Action<IntVec3>(IncrementAvoidGrid);

            private static void IncrementAvoidGrid(IntVec3 cell)
            {
                instance.IncrementAvoidGrid(cell, 45);
            }
        }
    }
}
