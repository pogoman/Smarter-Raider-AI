﻿using HarmonyLib;
using Mono.Unix.Native;
using PogoAI.Extensions;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using static RimWorld.BreachingUtility;

namespace PogoAI.Patches
{
    public class BreachingUtility
    {
        public static bool breachMineables = false;
        public static bool enforceMinimumRange = true;
        public static bool doneReset = false;

        public static class BreachRangedCastPositionFinder_SafeForRangedCast
        {
            //Everything here needs to be efficient, called 100000s times
            static bool Prefix(IntVec3 c, ref bool __result, object __instance)
            {
                var instance = Traverse.Create(__instance);
                var verb = instance.Field("verb").GetValue<Verb>();
                var map = instance.Field("breachingGrid").GetValue<BreachingGrid>().Map;
                if (!c.InBounds(map) || !c.Walkable(map))
                { 
                    __result = false;
                    return false;
                }
                __result = true;
                if (verb == null)
                {
                    return false;
                }

                //Check weapon min range in case of splash (cheaper than original code)
                ThingDef projectile = verb.GetProjectile();
                float modifier = 10;
                if (projectile != null && projectile.projectile.explosionRadius > 0f)
                {
                    if (verb.EquipmentCompSource?.parent?.def.thingCategories.FirstOrDefault()?.defName == "Grenades")
                    {
                        modifier = 1.5f;
                    }
                    else
                    {
                        modifier = 5;
                    }
                }
                var target = instance.Field("target").GetValue<Thing>();
                var effective = verb.EffectiveRange * verb.EffectiveRange / modifier;
                __result = !enforceMinimumRange || target.Position.DistanceToSquared(c) > effective;

                //Check for nearby reserved firingpos in case of FF in CE (mainly a problem for cents)
                if (__result && verb.EffectiveRange > 30)
                {
                    var reservedDestinations = Traverse.Create(map.pawnDestinationReservationManager).Field("reservedDestinations").GetValue<Dictionary<Faction, PawnDestinationReservationManager.PawnDestinationSet>>();
                    if (reservedDestinations.ContainsKey(verb.Caster.Faction))
                    {
                        var reservations = reservedDestinations[verb.Caster.Faction]
                           .list.Where(x => x.job?.def == JobDefOf.UseVerbOnThing && x.claimant.GetLord() == ((Pawn)verb.Caster).GetLord());
                        foreach (var reservation in reservations)
                        {
                            var num = (float)(c - reservation.target).LengthHorizontalSquared;
                            if ((projectile.projectile.explosionRadius == 0f || num < 100f) && PointsCollinear(c, reservation.target, target.Position, 1))
                            {
                                __result = false;
                                break;
                            }
                        }
                    }
                }
                return false;
            }

            public static bool PointsCollinear(IntVec3 a, IntVec3 b, IntVec3 c, float tolerance)
            {
                if (b.x - a.x == 0 && c.x - a.x == 0)
                {
                    return true;
                }
                float slopeAB = (b.z - a.z) / (a.x == b.x ? float.PositiveInfinity : (b.x - a.x));
                float slopeAC = (c.z - a.z) / (a.x == c.x ? float.PositiveInfinity : (c.x - a.x));
                var difference = Math.Abs(slopeAB - slopeAC);
#if DEBUG
                Find.CurrentMap.debugDrawer.FlashCell(a, 0.2f, $"{difference}", 60);
#endif

                return difference < tolerance;
            }

        }

        public static class BreachRangedCastPositionFinder_TryFindRangedCastPosition
        {
            static void Postfix(Pawn pawn, ref bool __result)
            {
                var lord = pawn.GetLord();
                Log.Message($"result {__result} {lord.ownedPawns.Any(x => x.CurJob?.def == JobDefOf.UseVerbOnThing)}");
                if (!__result && !lord.ownedPawns.Any(x => x.CurJob?.def == JobDefOf.UseVerbOnThing))
                {
                    var data = LordDataFor(lord);
                    data.Reset();
#if DEBUG
                    Log.Message("Could not find breach cast pos for any breacher so resetting breach data");
#endif
                    if (enforceMinimumRange)
                    {
                        enforceMinimumRange = false;
#if DEBUG
                        Log.Message("Could not find breach cast pos so disabling minimum range check");
#endif
                    } else if (doneReset && !breachMineables)
                    {
                        breachMineables = true;
#if DEBUG
                        Log.Message("Could not find cast after reset and no minrange so breachMineables");
#endif
                    }
                    doneReset = true;
                }
            }
        }

        [HarmonyPatch(typeof(Verse.AI.BreachingGrid), "FindBuildingToBreach")]
        static class BreachingUtility_FindBuildingToBreach
        {
            static void Postfix(ref Thing __result)
            {
                if (__result == null && !breachMineables)
                {
                    breachMineables = true;
#if DEBUG
                    Log.Message("Could not find breach building so breachMineables");
#endif
                }
            }
        }

        [HarmonyPatch(typeof(RimWorld.BreachingUtility), "BlocksBreaching")]
        static class BreachingUtility_BlocksBreaching
        {
            static void Postfix(Map map, IntVec3 c, ref bool __result)
            {
                if (__result)
                {
                    Building edifice = c.GetEdifice(map);
                    __result = edifice?.Faction == Faction.OfPlayer || (breachMineables && edifice.def.mineable);
                }
            }
        }

        [HarmonyPatch(typeof(RimWorld.BreachingUtility), "EscortRadius")]
        static class BreachingUtility_EscortRadius
        {
            static void Postfix(ref float __result)
            {
                __result *= 3;
            }
        }

        [HarmonyPatch(typeof(RimWorld.BreachingUtility), "IsSoloAttackVerb")]
        static class BreachingUtility_IsSoloAttackVerb
        {
            static bool Prefix(ref bool __result)
            {
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(RimWorld.BreachingUtility), "FindVerbToUseForBreaching")]
        static class BreachingUtility_FindVerbToUseForBreaching
        {
            static bool Prefix(Pawn pawn, ref Verb __result)
            {
                if (pawn.CurJobDef?.defName == "TendSelf")
                {
                    return false;
                }
                Pawn_EquipmentTracker equipment = pawn.equipment;
                CompEquippable compEquippable = (equipment != null) ? equipment.PrimaryEq : null;
                if (compEquippable == null)
                {
                    return false;
                }

                var weapon = compEquippable.ToString();

                var breachWeapons = Init.settings.breachWeapons.Replace(" ", string.Empty).Split(',');
                if (breachWeapons.Any(x => weapon.Matches(x)))
                {
                    if (Init.combatExtended)
                    {
                        if (new string[] { "inferno", "chargeblast", "thermal", "thump" }.Any(
                            x => weapon.Matches(x)))
                        {
                            if (!pawn.inventory.innerContainer.Any(x => x.ToString().Matches("ammo")))
                            {
                                return false;
                            }
                        }
                    }

                    if (!compEquippable.PrimaryVerb.verbProps.ai_IsBuildingDestroyer)
                    {
                        if (pawn.Faction == Faction.OfMechanoids || equipment.Primary.def.weaponTags.Any(x => x.Matches("GunSingleUse")))
                        {
                            compEquippable.PrimaryVerb.verbProps.ai_IsBuildingDestroyer = true;
                        }
                    }
                    if (equipment.Primary.def.weaponTags.Any(x => x.Matches("grenade")))
                    {
                        compEquippable.PrimaryVerb.verbProps.ai_IsBuildingDestroyer = false;
                    }

                    __result = compEquippable.PrimaryVerb;                    
                    return false;
                }

                return false;
            }
        }
    }
}
