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
            static bool Prefix(RimWorld.LordToil_AssaultColonyBreaching __instance)
            {
                if (!__instance.lord.ownedPawns.Any<Pawn>())
                {
                    return false;
                }
                if (__instance.Data?.breachDest != null && __instance.Data.breachDest.IsValid)
                {
                    if (__instance.Data.currentTarget == null)
                    {
                        return false;
                    }
                    Pawn checkWith;
                    __instance.lord.ownedPawns.TryRandomElement<Pawn>(out checkWith);
                    using (PawnPath breachPath = __instance.Map.pathFinder.FindPath(__instance.Data.breachStart, __instance.Data.breachDest,
                        TraverseParms.For(checkWith, Danger.Deadly, TraverseMode.PassAllDestroyableThings, false, true, false), PathEndMode.OnCell, null))
                    {
                        using (PawnPath pathNoBreach = __instance.Map.pathFinder.FindPath(__instance.Data.breachStart, __instance.Data.breachDest,
                        TraverseParms.For(checkWith, Danger.Deadly, TraverseMode.ByPawn, false, true, false), PathEndMode.OnCell, null))
                        {
                            Log.Message($"path brach {breachPath.TotalCost} path clear {pathNoBreach.TotalCost}");
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

                if (!__instance.Data.breachDest.IsValid)
                {
                    __instance.Data.Reset();
                    __instance.Data.preferMelee = Rand.Chance(0.5f);
                    __instance.Data.breachStart = __instance.lord.ownedPawns[0].PositionHeld;
                    __instance.Data.breachDest = GenAI.RandomRaidDest(__instance.Data.breachStart, __instance.Map);
                    int breachRadius = Mathf.RoundToInt(RimWorld.LordToil_AssaultColonyBreaching.BreachRadiusFromNumRaiders.Evaluate((float)__instance.lord.ownedPawns.Count));
                    int walkMargin = Mathf.RoundToInt(RimWorld.LordToil_AssaultColonyBreaching.WalkMarginFromNumRaiders.Evaluate((float)__instance.lord.ownedPawns.Count));
                    __instance.Data.breachingGrid.CreateBreachPath(__instance.Data.breachStart, __instance.Data.breachDest, breachRadius, walkMargin, __instance.useAvoidGrid);
                }

                return true;
            }

            static void Postfix(RimWorld.LordToil_AssaultColonyBreaching __instance)
            {
                Log.Message($"pawnsMeleeDestructive {__instance.pawnsMeleeDestructive.Count}");
                Log.Message($"pawnsRangedDestructive {__instance.pawnsRangedDestructive.Count}");
                Log.Message($"pawnsRangedGeneral {__instance.pawnsRangedGeneral.Count}");
                Log.Message($"pawnSoloAttackers {__instance.pawnSoloAttackers.Count}");
                Log.Message($"pawnsEscort {__instance.pawnsEscort.Count}");
                Log.Message($"pawnsLost {__instance.pawnsLost.Count}");

                __instance.Data.maxRange = 32f;
            }
        }
    }
}
