using HarmonyLib;
using Mono.Unix.Native;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using static UnityEngine.GraphicsBuffer;
using static Verse.AI.BreachingGrid;

namespace PogoAI.Patches
{
    internal class LordToil_AssaultColonyBreaching
    {
        [HarmonyPatch(typeof(RimWorld.LordToil_AssaultColonyBreaching), "UpdateAllDuties")]
        static class LordToil_AssaultColonyBreaching_UpdateAllDuties
        {
            public static bool Prefix(RimWorld.LordToil_AssaultColonyBreaching __instance)
            {
                if (__instance.Data?.breachDest != null && __instance.Data.breachDest.IsValid)
                {
                    if (!__instance.lord.ownedPawns.Any(x => x.mindState.duty.def == DutyDefOf.Breaching)) {
                        return false;
                    }
                    using (PawnPath breachPath = __instance.Map.pathFinder.FindPath(__instance.Data.breachStart, __instance.Data.breachDest,
                        TraverseParms.For(__instance.lord.ownedPawns[0], Danger.Deadly, TraverseMode.PassAllDestroyableThings, false, true, false), PathEndMode.OnCell, null))
                    {
                        using (PawnPath pathNoBreach = __instance.Map.pathFinder.FindPath(__instance.Data.breachStart, __instance.Data.breachDest,
                        TraverseParms.For(__instance.lord.ownedPawns[0], Danger.Deadly, TraverseMode.ByPawn, false, true, false), PathEndMode.OnCell, null))
                        {
                            //Log.Message($"path brach {breachPath.TotalCost} path clear {pathNoBreach.TotalCost}");
                            if (Math.Abs(breachPath.TotalCost - pathNoBreach.TotalCost) < 1000)
                            {
                                foreach (var pawn in __instance.lord.ownedPawns)
                                {
                                    if (pawn.mindState.duty.def == DutyDefOf.Breaching || pawn.mindState.duty.def == DutyDefOf.Escort)
                                    {
                                        pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
                                    }
                                }
                                __instance.Data.currentTarget = null;
                                //Log.Message("Breach not required have clear path");
                                return false;
                            }
                        }
                    }
                }
                return true;
            }

            public static void Postfix(RimWorld.LordToil_AssaultColonyBreaching __instance)
            {
                Log.Message($"pawnsMeleeDestructive {__instance.pawnsMeleeDestructive.Count}");
                Log.Message($"pawnsRangedDestructive {__instance.pawnsRangedDestructive.Count}");
                Log.Message($"pawnsRangedGeneral {__instance.pawnsRangedGeneral.Count}");
                Log.Message($"pawnSoloAttackers {__instance.pawnSoloAttackers.Count}");
                Log.Message($"pawnsEscort {__instance.pawnsEscort.Count}");
                Log.Message($"pawnsLost {__instance.pawnsLost.Count}");

                var breachers = __instance.lord.ownedPawns.Where(x => x.mindState.duty.def == DutyDefOf.Breaching);
                Log.Message($"breac dest: {__instance.Data.breachDest} breach target: {__instance.Data.currentTarget} breachers {breachers.Count()}");
                if (breachers.Count() == 0 && __instance.Data.currentTarget != null)
                {
                    foreach (var pawn in __instance.lord.ownedPawns)
                    {
                        if (IsPogoBreacher(pawn))
                        {
                            pawn.mindState.duty = new PawnDuty(DutyDefOf.Breaching, __instance.Data.breachDest, -1f);
                        }
                    }
                }

                __instance.Data.maxRange = 144f;
            }

            private static bool IsPogoBreacher(Pawn pawn)
            {
                Pawn_EquipmentTracker equipment = pawn.equipment;
                CompEquippable compEquippable = (equipment != null) ? equipment.PrimaryEq : null;
                if (compEquippable == null)
                {
                    return false;
                }
                Verb primaryVerb = compEquippable.PrimaryVerb;
                if (BreachingUtility.UsableVerb(primaryVerb) && primaryVerb.verbProps.ai_IsBuildingDestroyer)
                {
                    return true;
                }

                if (new string[] { "Stick", "Concussion", "Rocket", "Inferno", "Blast" }.Any(
                    x => compEquippable.ToString().IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return true;
                }

                return false;
            }
        }

    }
}
