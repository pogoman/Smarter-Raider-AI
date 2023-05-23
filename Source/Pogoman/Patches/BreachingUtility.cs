using HarmonyLib;
using Mono.Unix.Native;
using PogoAI.Extensions;
using RimWorld;
using System;
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

        [HarmonyPatch(typeof(BreachRangedCastPositionFinder), "SafeForRangedCast")]
        static class BreachRangedCastPositionFinder_SafeForRangedCast
        {
            //Everything here needs to be efficient, called 100000s times
            static bool Prefix(IntVec3 c, ref bool __result, BreachRangedCastPositionFinder __instance)
            {
                if (!SafeUseableFiringPosition(__instance.breachingGrid, c))
                {
                    __result = false;
                    return false;
                }
                __result = true;
                if (__instance.verb == null)
                {
                    return false;
                }

                //Check weapon min range in case of splash (cheaper than original code)
                ThingDef projectile = __instance.verb.GetProjectile();
                float modifier = 10;
                if (projectile != null && projectile.projectile.explosionRadius > 0f)
                {
                    if (__instance.verb.EquipmentCompSource?.parent?.def.thingCategories.FirstOrDefault()?.defName == "Grenades")
                    {
                        modifier = 1.5f;
                    }
                    else
                    {
                        modifier = 5;
                    }
                }
                var effective = __instance.verb.EffectiveRange * __instance.verb.EffectiveRange / modifier;
                __result = !enforceMinimumRange || __instance.target.Position.DistanceToSquared(c) > effective;

                //Check for nearby reserved firingpos in case of FF in CE (mainly a problem for cents)
                if (__result && __instance.verb.EffectiveRange > 30)
                {
                    if (__instance.breachingGrid.map.pawnDestinationReservationManager.reservedDestinations.ContainsKey(__instance.verb.Caster.Faction))
                    {
                        var reservations = __instance.breachingGrid.map.pawnDestinationReservationManager.reservedDestinations[__instance.verb.Caster.Faction]
                           .list.Where(x => x.job?.def == JobDefOf.UseVerbOnThing);
                        foreach (var reservation in reservations)
                        {
                            var num = (float)(c - reservation.target).LengthHorizontalSquared;
                            if ((projectile.projectile.explosionRadius == 0f || num < 100f) && PointsCollinear(c, reservation.target, __instance.target.Position, 10))
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
                float slopeAB = Math.Abs(a.x == b.x ? float.PositiveInfinity : (b.y - a.y) / (b.x - a.x));
                float slopeAC = Math.Abs(a.x == c.x ? float.PositiveInfinity : (c.y - a.y) / (c.x - a.x));

                return (slopeAC != float.PositiveInfinity && slopeAB != float.PositiveInfinity && slopeAB - slopeAC > tolerance) || slopeAB - slopeAC == 0;
            }

        }

        [HarmonyPatch(typeof(BreachRangedCastPositionFinder), "TryFindRangedCastPosition")]
        static class BreachRangedCastPositionFinder_TryFindRangedCastPosition
        {
            static void Postfix(Pawn pawn, ref bool __result)
            {
                var lord = pawn.GetLord();
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
                    }
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

        [HarmonyPatch(typeof(BreachingGrid), "CreateBreachPath")]
        static class BreachingGrid_CreateBreachPath
        {
            static void Prefix(ref int breachRadius, ref int walkMargin)
            {
                //breachRadius = breachRadius * 3;
                walkMargin = walkMargin * 5;
            }
        }

        [HarmonyPatch(typeof(Verse.AI.BreachingGrid), "FindBuildingToBreach")]
        static class FindBuildingToBreach_FindBuildingToBreach
        {
            static void Postfix(ref Thing __result)
            {
                if (__result == null && !breachMineables)
                {
                    breachMineables = true;
#if DEBUG
                    Log.Message("Could not find breach building so enabling breachMineables");
#endif
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
