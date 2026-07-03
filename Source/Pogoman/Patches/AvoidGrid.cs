using HarmonyLib;
using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PogoAI.Patches
{
    //The 1.6 debug overlay draws the raw grid field, so it never triggers the lazy
    //regenerate; touch the Grid property while dirty so the overlay stays fresh.
    [HarmonyPatch(typeof(Verse.AI.AvoidGrid), "DebugDrawOnMap")]
    public static class AvoidGrid_DebugDrawOnMap
    {
        static void Prefix(Verse.AI.AvoidGrid __instance)
        {
            if (DebugViewSettings.drawAvoidGrid && Find.CurrentMap == __instance.map && __instance.gridDirty)
            {
                _ = __instance.Grid;
            }
        }
    }

    [HarmonyPatch(typeof(Verse.AI.AvoidGrid), "Regenerate")]
    public static class AvoidGrid_Regenerate
    {
        public const int UpdateIntervalTicks = 300;

        private class CETurretAccessors
        {
            public readonly PropertyInfo gunCompEq;
            public readonly PropertyInfo active;
            public readonly PropertyInfo powerComp;
            public readonly PropertyInfo currentTarget;
            public readonly PropertyInfo emptyMagazine;

            public CETurretAccessors(Type type)
            {
                gunCompEq = type.GetProperty("GunCompEq");
                active = type.GetProperty("Active");
                powerComp = type.GetProperty("PowerComp");
                currentTarget = type.GetProperty("CurrentTarget");
                emptyMagazine = type.GetProperty("EmptyMagazine");
            }

            public bool Valid => gunCompEq != null && active != null && powerComp != null
                && currentTarget != null && emptyMagazine != null;
        }

        private static readonly Dictionary<Type, CETurretAccessors> ceTurretAccessors = new Dictionary<Type, CETurretAccessors>();
        private static bool ceTurretWarned;

        // Scratch grid marking cells already counted for the current LOS source,
        // reused between calls to avoid allocating a map-sized grid per turret.
        private static ByteGrid visitedGrid;

        static bool Prefix(Verse.AI.AvoidGrid __instance)
        {
            var comp = PogoMapComponent.For(__instance.map);
            if (comp == null)
            {
                return true;
            }

            //No need to update more often than every 5 seconds. Leave gridDirty set while
            //throttled so the first grid access after the cooldown expires regenerates.
            if (comp.lastAvoidGridUpdateTicks != 0 && Find.TickManager.TicksGame - comp.lastAvoidGridUpdateTicks < UpdateIntervalTicks)
            {
                return false;
            }
            __instance.gridDirty = false;

            __instance.grid.Clear();

            try
            {
                //Corpses
                var corpses = __instance.map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).Where(x => ((Corpse)x).Age < 1800
                    && (((Corpse)x).InnerPawn?.Faction?.HostileTo(Faction.OfPlayer) ?? false));
                foreach (Corpse corpse in corpses)
                {
                    PrintAvoidGridAroundPos(__instance, __instance.map, corpse.Position, 1, 1000 * (1800 - corpse.Age) / 1800);
                }

                //Downed Raiders
                var downed = __instance.map.mapPawns.SpawnedDownedPawns.Where(x => x.Faction.HostileTo(Faction.OfPlayer));
                foreach (var raider in downed)
                {
                    PrintAvoidGridAroundPos(__instance, __instance.map, raider.Position, 1);
                }

                //Colonist Pawns
                var draftedColonists = __instance.map.PlayerPawnsForStoryteller.Where(x =>
                    x.Drafted && x.equipment?.PrimaryEq != null && x.CurJobDef == JobDefOf.Wait_Combat && x.TargetCurrentlyAimingAt == null);
                foreach (var pawn in draftedColonists)
                {
                    var verb = pawn.equipment.PrimaryEq.PrimaryVerb;
                    if (verb.IsMeleeAttack)
                    {
                        PrintAvoidGridAroundPos(__instance, __instance.map, pawn.Position, 1);
                    }
                    else
                    {
                        PrintAvoidGridLOSThing(__instance, pawn.Map, pawn.Position, verb);
                    }
                }

                //Turrets
                List<Building> allBuildingsColonist = __instance.map.listerBuildings.allBuildingsColonist;
                for (int i = 0; i < allBuildingsColonist.Count; i++)
                {
                    var building = allBuildingsColonist[i];
                    if (!building.def.building.ai_combatDangerous)
                    {
                        continue;
                    }
                    CompEquippable equip = null;
                    var threatCondition = false;
                    if (Init.combatExtended && building.GetType().ToString().Contains("CombatExtended"))
                    {
                        var accessors = GetCETurretAccessors(building.GetType());
                        if (accessors != null)
                        {
                            try
                            {
                                equip = (CompEquippable)accessors.gunCompEq.GetValue(building, null);
                                var active = (bool)accessors.active.GetValue(building, null);
                                var activePowerSource = (CompPowerTrader)accessors.powerComp.GetValue(building, null);
                                var currentTarget = (LocalTargetInfo)accessors.currentTarget.GetValue(building, null);
                                var emptyMagazine = (bool)accessors.emptyMagazine.GetValue(building, null);
                                threatCondition = (active || (activePowerSource?.PowerNet?.CanPowerNow(activePowerSource) ?? false))
                                    && currentTarget == null && !emptyMagazine && equip != null;
                            }
                            catch (Exception e)
                            {
                                WarnCETurretOnce(building.GetType(), e);
                            }
                        }
                    }
                    else if (building is Building_TurretGun building_TurretGun)
                    {
                        equip = building_TurretGun.GunCompEq;
                        threatCondition = equip != null && (building_TurretGun.Active
                            || (building_TurretGun.powerComp?.PowerNet?.CanPowerNow(building_TurretGun.powerComp) ?? false))
                            && building_TurretGun.TargetCurrentlyAimingAt == null
                            && (building_TurretGun.refuelableComp?.HasFuel ?? true);
                    }
                    if (threatCondition && equip != null)
                    {
                        PrintAvoidGridLOSThing(__instance, building.Map, building.Position, equip.PrimaryVerb);
                    }
                }
                __instance.ExpandAvoidGridIntoEdifices();
            }
            catch (Exception e)
            {
                Log.Error($"Smarter Raid AI: {e.Message}\n{e.StackTrace}");
            }

            comp.lastAvoidGridUpdateTicks = Find.TickManager.TicksGame;
            //The 1.6 pathfinder bakes the avoid grid into cached per-request cost grids
            //that only rebuild on cell deltas; queue one so they pick up the new values.
            __instance.map.pathFinder?.MapData.Notify_CellDelta(IntVec3.Zero);
            return false;
        }

        private static CETurretAccessors GetCETurretAccessors(Type type)
        {
            if (!ceTurretAccessors.TryGetValue(type, out var accessors))
            {
                accessors = new CETurretAccessors(type);
                if (!accessors.Valid)
                {
                    accessors = null;
                    WarnCETurretOnce(type, null);
                }
                ceTurretAccessors[type] = accessors;
            }
            return accessors;
        }

        private static void WarnCETurretOnce(Type type, Exception e)
        {
            if (!ceTurretWarned)
            {
                ceTurretWarned = true;
                Log.Warning($"SRAI: Combat Extended turret type {type} did not have the expected members, skipping it for avoid grid. {e}");
            }
        }

        private static void PrintAvoidGridLOSThing(Verse.AI.AvoidGrid avoidGrid, Map map, IntVec3 pos, Verb verb)
        {
            if (verb.Caster.def.defName == "Turret_RocketswarmLauncher")
            {
                return;
            }
            if (visitedGrid == null)
            {
                visitedGrid = new ByteGrid(map);
            }
            else
            {
                visitedGrid.ClearAndResizeTo(map);
            }
            int cost = Init.settings.costLOS;
            Func<IntVec3, bool> markCell = cell =>
            {
                if (visitedGrid[cell] == 0)
                {
                    avoidGrid.IncrementAvoidGrid(cell, cost);
                    visitedGrid[cell] = (byte)Mathf.Min(255, visitedGrid[cell] + cost);
                }
                return true;
            };
            float range = verb.verbProps.range;
            float num = verb.verbProps.EffectiveMinRange(true);
            int num2 = GenRadial.NumCellsInRadius(range);
            for (int i = num2; i > (num < 1f ? 0 : GenRadial.NumCellsInRadius(num)); i--)
            {
                IntVec3 intVec = pos + GenRadial.RadialPattern[i];
                if (intVec.InBounds(map) && intVec.WalkableByNormal(map)
                    && visitedGrid[intVec] == 0)
                {
                    GenSight.LineOfSight(pos, intVec, map, true, markCell, 0, 0);
                }
            }
        }

        public static void PrintAvoidGridAroundPos(Verse.AI.AvoidGrid avoidGrid, Map map, IntVec3 pos, int radius, int incAmount = -1)
        {
            if (incAmount == -1)
            {
                incAmount = Init.settings.costLOS;
            }
            for (int i = 0; i < GenRadial.NumCellsInRadius(radius); i++)
            {
                IntVec3 intVec = pos + GenRadial.RadialPattern[i];
                if (intVec.InBounds(map) && intVec.WalkableByNormal(map)
                    && avoidGrid.Grid[map.cellIndices.CellToIndex(intVec)] == 0)
                {
                    avoidGrid.IncrementAvoidGrid(intVec, incAmount);
                }
            }
        }
    }
}
