using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using static RimWorld.BreachingUtility;
using static UnityEngine.GraphicsBuffer;

namespace PogoAI.Patches
{
    internal class BreachingUtility
    {
        [HarmonyPatch(typeof(BreachRangedCastPositionFinder), "SafeForRangedCast")]
        static class BreachRangedCastPositionFinder_SafeForRangedCast
        {
            static bool Prefix(IntVec3 c, ref bool __result, BreachRangedCastPositionFinder __instance)
            {
                CellRect occupiedRect = CellRect.SingleCell(__instance.target.Position);
                var distance = occupiedRect.ClosestDistSquaredTo(c);
                var squaredEffectiveThird = __instance.verb.EffectiveRange * __instance.verb.EffectiveRange / 4;
                __result = distance > squaredEffectiveThird;
                return false;
            }
        }

        [HarmonyPatch(typeof(RimWorld.BreachingUtility), "BlocksBreaching")]
        static class BreachingUtility_BlocksBreaching
        {
            static void Postfix(Map map, IntVec3 c, ref bool __result)
            {
                if (__result)
                {
                    __result = GenClosest.ClosestThing_Global(c, map.listerBuildings.allBuildingsColonist, 10f) != null;
                }
            }
        }

        [HarmonyPatch(typeof(RimWorld.BreachingUtility), "EscortRadius")]
        static class BreachingUtility_EscortRadius
        {
            static void Postfix(ref float __result)
            {
                __result *= 10;
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
                Pawn_EquipmentTracker equipment = pawn.equipment;
                CompEquippable compEquippable = (equipment != null) ? equipment.PrimaryEq : null;
                if (compEquippable == null)
                {
                    return false;
                }

                if (Init.combatExtended)
                {
                    if (compEquippable.ToString().IndexOf("flame", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return false;
                    }
                }
                var breachWeapons = PogoSettings.DEFAULT_BREACH_WEAPONS.Replace(" ", string.Empty).Split(',');
                if (breachWeapons.Any(x => compEquippable.ToString().IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    if (Init.combatExtended)
                    {
                        if (new string[] { "Inferno", "Blast", "Thermal", "Thump" }.Any(
                            x => compEquippable.ToString().IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            if (!pawn.inventory.innerContainer.Any(x => x.ToString().Contains("Ammo")))
                            {
                                return false;
                            }
                        }
                    }
                    __result = compEquippable.PrimaryVerb;
                    if (compEquippable.PrimaryVerb.verbProps.ai_IsBuildingDestroyer && !pawn.RaceProps.IsMechanoid)
                    {
                        compEquippable.PrimaryVerb.verbProps.ai_IsBuildingDestroyer = false;
                    }
                    return false;
                }

                return false;
            }
        }
    }
}
