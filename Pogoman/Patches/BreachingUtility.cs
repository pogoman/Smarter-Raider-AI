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
        static class BreachingUtility_SafeForRangedCast
        {
            static bool Prefix(IntVec3 c, ref bool __result, BreachRangedCastPositionFinder __instance)
            {
                ShootLine shootLine; 
                var cellsInFront = LastBresenhamCellsBetweenLimit(c, __instance.target.Position, 5);
                __result = __instance.verb.TryFindShootLineFromTo(c, cellsInFront, out shootLine);
                //__result = __instance.verb.TryFindShootLineFromTo(c, __instance.target.Position, out shootLine);
                if (__result)
                    Log.Message($"check: {c}, {__instance.target} {__result} {cellsInFront}");
                return false;
            }
        }

        static bool CellsInRadiusClear(IntVec3 c, int radius, Map map)
        {
            for (int i = 0; i < 8; i++)
            {
                IntVec3 c2 = c + GenAdj.AdjacentCellsAround[i];
                if (c2.GetThingList(map).Any())
                {
                    return false;
                }
            }
            if (radius == 0)
            {
                return true;
            }
            else
            {
                return CellsInRadiusClear(c, radius - 1, map);
            }
        }

        private static IntVec3 LastBresenhamCellsBetweenLimit(IntVec3 a, IntVec3 b, int limit)
        {
            return LastBresenhamCellsBetweenLimit(a.x, a.z, b.x, b.z, limit);
        }

        // Token: 0x06002AEE RID: 10990 RVA: 0x001129D8 File Offset: 0x00110BD8
        private static IntVec3 LastBresenhamCellsBetweenLimit(int x0, int y0, int x1, int y1, int limit)
        {
            IntVec3 tmpCell;
            int num = Mathf.Abs(x1 - x0);
            int num2 = (x0 < x1) ? 1 : -1;
            int num3 = -Mathf.Abs(y1 - y0);
            int num4 = (y0 < y1) ? 1 : -1;
            int num5 = num + num3;
            int num6 = limit;
            do
            {
                tmpCell = new IntVec3(x0, 0, y0);
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }
                int num7 = 2 * num5;
                if (num7 >= num3)
                {
                    num5 += num3;
                    x0 += num2;
                }
                if (num7 <= num)
                {
                    num5 += num;
                    y0 += num4;
                }
                num6--;
            }
            while (num6 > 0);
            return tmpCell;
        }

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
                if (Init.CombatExtended != null)
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
                //Log.Message($"ce {Init.CombatExtended} pawn {pawn} equip {compEquippable} verb {compEquippable.PrimaryVerb}");
                if (compEquippable == null)
                {
                    return false;
                }

                if (Init.CombatExtended != null)
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
                    if (Init.CombatExtended != null)
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

                if (Init.CombatExtended != null)
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
