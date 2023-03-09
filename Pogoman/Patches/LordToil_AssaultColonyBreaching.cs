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
        //NNEED CASE FOR NO BREACHERS
        [HarmonyPatch(typeof(RimWorld.LordToil_AssaultColonyBreaching), "UpdateAllDuties")]
        static class LordToil_AssaultColonyBreaching_UpdateAllDuties
        {
            static bool Prefix(RimWorld.LordToil_AssaultColonyBreaching __instance)
            {
                if (!__instance.lord.ownedPawns.Any<Pawn>())
                {
                    return false;
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
                __instance.Data.maxRange = 144f;
                if (__instance.Data?.breachDest != null && __instance.Data.breachDest.IsValid)
                {
                    if (__instance.Data.currentTarget == null) {
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

                    foreach (var pawn in __instance.lord.ownedPawns)
                    {
                        if (IsPogoBreacher(pawn))
                        {
                            pawn.mindState.duty = new PawnDuty(DutyDefOf.Breaching, __instance.Data.breachDest, -1f);
                        }
                        else
                        {
                            pawn.mindState.duty = new PawnDuty(DutyDefOf.Escort, __instance.lord.ownedPawns.RandomElement());
                        }
                    }
                }
                return false;
            }

            //static void Postfix(RimWorld.LordToil_AssaultColonyBreaching __instance)
            //{
            //    Log.Message($"pawnsMeleeDestructive {__instance.pawnsMeleeDestructive.Count}");
            //    Log.Message($"pawnsRangedDestructive {__instance.pawnsRangedDestructive.Count}");
            //    Log.Message($"pawnsRangedGeneral {__instance.pawnsRangedGeneral.Count}");
            //    Log.Message($"pawnSoloAttackers {__instance.pawnSoloAttackers.Count}");
            //    Log.Message($"pawnsEscort {__instance.pawnsEscort.Count}");
            //    Log.Message($"pawnsLost {__instance.pawnsLost.Count}");

            //    var breachers = __instance.lord.ownedPawns.Where(x => x.mindState.duty.def == DutyDefOf.Breaching);
            //    //Log.Message($"breac dest: {__instance.Data.breachDest} breach target: {__instance.Data.currentTarget} breachers {breachers.Count()}");
            //    if (__instance.Data.currentTarget != null)
            //    {
            //        foreach (var pawn in __instance.lord.ownedPawns)
            //        {
            //            var isBreacher = IsPogoBreacher(pawn);
            //            if (IsPogoBreacher(pawn))
            //            {
            //                if (pawn.mindState.duty.def != DutyDefOf.Breaching)
            //                {
            //                    pawn.mindState.duty = new PawnDuty(DutyDefOf.Breaching, __instance.Data.breachDest, -1f);
            //                }
            //            }
            //            else
            //            {
            //                if (pawn.mindState.duty.def == DutyDefOf.Breaching)
            //                {
            //                    pawn.mindState.duty = new PawnDuty(DutyDefOf.Escort, __instance.lord.ownedPawns.RandomElement());
            //                }
            //            }
            //        }
            //    }
            //    else
            //    {
            //        //var ranged = breachers.Where(x => x.equipment.PrimaryEq?.PrimaryVerb?.verbProps?.range != null);
            //        //if (ranged.Count() > 0)
            //        //{
            //        //    var averageRange = ranged.Average(x => x.equipment.PrimaryEq.PrimaryVerb.verbProps.range);
            //        //    foreach (var breacher in breachers)
            //        //    {
            //        //        var range = breacher.equipment.PrimaryEq?.PrimaryVerb?.verbProps?.range;
            //        //        if (range != null)
            //        //        {
            //        //            if (Math.Abs(range.Value - averageRange) > 50)
            //        //            {
            //        //                breacher.mindState.duty = new PawnDuty(DutyDefOf.Escort, breachers.RandomElement<Pawn>(),
            //        //                    RimWorld.BreachingUtility.EscortRadius(breacher));
            //        //            }
            //        //        }
            //        //    }
            //        //}
                    
            //    }

            //    __instance.Data.maxRange = 144f;
            //}

            //[HarmonyPatch(typeof(RimWorld.LordToil_AssaultColonyBreaching), "SetBreachDuty")]
            //static class LordToil_AssaultColonyBreaching_SetBreachDuty
            //{
            //    public static void Prefix(ref List<Pawn> breachers, RimWorld.LordToil_AssaultColonyBreaching __instance)
            //    {
            //        var pogoBreachers = new List<Pawn>();
            //        foreach (var breacher in breachers)
            //        {
            //            if (IsPogoBreacher(breacher))
            //            {
            //                pogoBreachers.Add(breacher);
            //            }
            //            else
            //            {
            //                __instance.pawnsEscort.Add(breacher);
            //            }
            //        }
            //        breachers = pogoBreachers;
            //    }
            //}

            static bool IsPogoBreacher(Pawn pawn)
            {
                Pawn_EquipmentTracker equipment = pawn.equipment;
                CompEquippable compEquippable = (equipment != null) ? equipment.PrimaryEq : null;
                if (compEquippable == null)
                {
                    return false;
                }
                Verb primaryVerb = compEquippable.PrimaryVerb;
                if (RimWorld.BreachingUtility.UsableVerb(primaryVerb) && primaryVerb.verbProps.ai_IsBuildingDestroyer)
                {
                    return true;
                }

                if (new string[] { "Stick", "Concussion", "Rocket", "Inferno", "Blast", "Thermal", "Breach", "Thump" }.Any(
                    x => compEquippable.ToString().IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return true;
                }

                return false;
            }
        }



    }
}
