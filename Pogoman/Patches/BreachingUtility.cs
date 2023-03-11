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

        [HarmonyPatch(typeof(RimWorld.BreachingUtility), "TryFindCastPosition")]
        static class BreachingUtility_TryFindCastPosition
        {
            static bool Prefix(ref bool __result, Pawn pawn, Verb verb, Thing target, out IntVec3 result)
            {
                __result = true;
                result = pawn.Position;
                return false;
            }
        }

        [HarmonyPatch(typeof(RimWorld.BreachingUtility), "EscortRadius")]
        static class BreachingUtility_EscortRadius
        {
            static void Postfix(ref float __result)
            {
                if (Init.CombatExtended)
                {
                    __result *= 4;
                }
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

                if (Init.CombatExtended)
                {
                    if (new string[] { "Flame", "Grenade Launcher" }.Any(
                                        x => compEquippable.ToString().IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        return false;
                    }
                }

                if (new string[] { "Stick", "Concussion", "Rocket", "Inferno", "Blast", "Thermal", "Thump" }.Any(
                    x => compEquippable.ToString().IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    if (Init.CombatExtended)
                    {
                        if (new string[] { "Inferno", "Blast", "Thermal", "Thump" }.Any(
                            x => compEquippable.ToString().IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            if (!pawn.inventory.innerContainer.Any(x => x.ToString().Contains("Ammo")))
                            {
                                Log.Message("no ammo");
                                return false;
                            }
                        }
                    }
                    __result = compEquippable.PrimaryVerb;
                    return false;
                }

                if (Init.CombatExtended)
                { 
                    if (pawn.inventory.innerContainer.Any(x => x.ToString().Contains("Ammo")))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
