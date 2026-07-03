using HarmonyLib;
using PogoAI.Extensions;
using RimWorld;
using System;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace PogoAI.Patches
{
    public class BreachingUtility
    {
        [HarmonyPatch(typeof(RimWorld.BreachingUtility.BreachRangedCastPositionFinder), "SafeForRangedCast")]
        public static class BreachRangedCastPositionFinder_SafeForRangedCast
        {
            //Everything here needs to be efficient, called 100000s times
            static void Postfix(IntVec3 c, ref bool __result, RimWorld.BreachingUtility.BreachRangedCastPositionFinder __instance)
            {
                if (!__result)
                {
                    return;
                }
                var comp = PogoMapComponent.For(__instance.breachingGrid?.Map);
                if (comp == null || !comp.enforceMinimumRange)
                {
                    return;
                }
                var verb = __instance.verb;
                if (verb == null)
                {
                    return;
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
                var target = __instance.target;
                var effective = verb.EffectiveRange * verb.EffectiveRange / modifier;
                __result = target.Position.DistanceToSquared(c) > effective;
            }
        }

        [HarmonyPatch(typeof(RimWorld.BreachingUtility.BreachRangedCastPositionFinder), "TryFindRangedCastPosition")]
        public static class BreachRangedCastPositionFinder_TryFindRangedCastPosition
        {
            static void Postfix(Pawn pawn, ref bool __result)
            {
                if (__result)
                {
                    return;
                }
                var lord = pawn.GetLord();
                var comp = PogoMapComponent.For(pawn.Map);
                if (lord == null || comp == null)
                {
                    return;
                }
                if (!lord.ownedPawns.Any(x => x.CurJob?.def == JobDefOf.UseVerbOnThing))
                {
                    var data = RimWorld.BreachingUtility.LordDataFor(lord);
                    data.Reset();
#if DEBUG
                    Log.Message("Could not find breach cast pos for any breacher so resetting breach data");
#endif
                    if (comp.enforceMinimumRange)
                    {
                        comp.enforceMinimumRange = false;
#if DEBUG
                        Log.Message("Could not find breach cast pos so disabling minimum range check");
#endif
                    }
                    else if (comp.doneReset && !comp.breachMineables)
                    {
                        comp.breachMineables = true;
#if DEBUG
                        Log.Message("Could not find cast after reset and no minrange so breachMineables");
#endif
                    }
                    comp.doneReset = true;
                }
            }
        }

        [HarmonyPatch(typeof(Verse.AI.BreachingGrid), "FindBuildingToBreach")]
        static class BreachingUtility_FindBuildingToBreach
        {
            static void Postfix(ref Thing __result, Verse.AI.BreachingGrid __instance)
            {
                var comp = PogoMapComponent.For(__instance.Map);
                if (__result == null && comp != null && !comp.breachMineables)
                {
                    comp.breachMineables = true;
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
                    var comp = PogoMapComponent.For(map);
                    __result = edifice?.Faction == Faction.OfPlayer
                        || (comp != null && comp.breachMineables && edifice != null && edifice.def.mineable);
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
            static string breachWeaponsRaw;
            static string[] breachWeapons;

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

                if (breachWeaponsRaw != Init.settings.breachWeapons)
                {
                    breachWeaponsRaw = Init.settings.breachWeapons;
                    breachWeapons = breachWeaponsRaw.Replace(" ", string.Empty).Split(',');
                }
                if (breachWeapons.Any(x => weapon.Matches(x)))
                {
                    if (Init.combatExtended && !HasAmmo(pawn.equipment.Primary))
                    {
                        return false;
                    }

                    var verbProps = compEquippable.PrimaryVerb.verbProps;
                    if (!verbProps.ai_IsBuildingDestroyer)
                    {
                        if (pawn.Faction == Faction.OfMechanoids || equipment.Primary.def.weaponTags.Any(x => x.Matches("GunSingleUse")))
                        {
                            Utilities.SetBuildingDestroyer(verbProps, true);
                        }
                    }
                    if (equipment.Primary.def.weaponTags.Any(x => x.Matches("grenade")))
                    {
                        Utilities.SetBuildingDestroyer(verbProps, false);
                    }

                    __result = compEquippable.PrimaryVerb;
                    return false;
                }

                return false;
            }

            static bool ceAmmoResolved;
            static Type ammoUserType;
            static PropertyInfo hasAmmoProp;
            static PropertyInfo curMagCountProp;
            static FieldInfo curMagCountField;

            public static bool HasAmmo(Thing gun)
            {
                if (!(gun is ThingWithComps twc))
                {
                    return false;
                }
                if (!ceAmmoResolved)
                {
                    ceAmmoResolved = true;
                    ammoUserType = AccessTools.TypeByName("CombatExtended.CompAmmoUser");
                    if (ammoUserType != null)
                    {
                        hasAmmoProp = AccessTools.Property(ammoUserType, "HasAmmo");
                        curMagCountProp = AccessTools.Property(ammoUserType, "CurMagCount");
                        curMagCountField = AccessTools.Field(ammoUserType, "CurMagCount");
                    }
                }
                if (ammoUserType == null)
                {
                    return false;
                }

                ThingComp comp = null;
                var comps = twc.AllComps;
                for (int i = 0; i < comps.Count; i++)
                {
                    if (ammoUserType.IsInstanceOfType(comps[i]))
                    {
                        comp = comps[i];
                        break;
                    }
                }
                if (comp == null)
                {
                    return false;
                }

                // Prefer HasAmmo property
                if (hasAmmoProp?.PropertyType == typeof(bool))
                {
                    return (bool)hasAmmoProp.GetValue(comp);
                }

                // Fallbacks
                if (curMagCountProp != null)
                {
                    return Convert.ToInt32(curMagCountProp.GetValue(comp)) > 0;
                }
                if (curMagCountField != null)
                {
                    return Convert.ToInt32(curMagCountField.GetValue(comp)) > 0;
                }

                return false;
            }
        }
    }
}
