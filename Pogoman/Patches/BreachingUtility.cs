using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using static RimWorld.BreachingUtility;
using static UnityEngine.GraphicsBuffer;

namespace PogoAI.Patches
{
    internal class BreachingUtility
    {
        [HarmonyPatch(typeof(RimWorld.BreachingUtility), "FindVerbToUseForBreaching")]
        static class BreachingUtility_FindVerbToUseForBreaching
        {
            static bool Prefix(Pawn pawn)
            {
                Pawn_EquipmentTracker equipment = pawn.equipment;
                CompEquippable compEquippable = (equipment != null) ? equipment.PrimaryEq : null;
                if (compEquippable == null)
                {
                    return false;
                }

                if (new string[] { "Flame" }.Any(
                    x => compEquippable.ToString().IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
