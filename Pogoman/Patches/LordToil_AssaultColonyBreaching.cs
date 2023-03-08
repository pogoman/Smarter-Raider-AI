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
using static UnityEngine.GraphicsBuffer;

namespace PogoAI.Patches
{
    internal class LordToil_AssaultColonyBreaching
    {
        [HarmonyPatch(typeof(RimWorld.LordToil_AssaultColonyBreaching), "UpdateAllDuties")]
        static class LordToil_AssaultColonyBreaching_UpdateAllDuties
        {
            public static bool Prefix(RimWorld.LordToil_AssaultColonyBreaching __instance)
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
                __instance.pawnsRangedDestructive.Clear();
                __instance.pawnsMeleeDestructive.Clear();
                __instance.pawnsRangedGeneral.Clear();
                __instance.pawnSoloAttackers.Clear();
                __instance.pawnsEscort.Clear();
                __instance.pawnsLost.Clear();
                __instance.Data.maxRange = 12f;
                for (int i = 0; i < __instance.lord.ownedPawns.Count; i++)
                {                    
                    Pawn pawn = __instance.lord.ownedPawns[i];
                    if (!pawn.CanReach(__instance.Data.breachStart, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
                    {
                        __instance.pawnsLost.Add(pawn);
                    }
                    else
                    {
                        Verb verb = LordToil_AssaultColonyBreaching_SetBreachDuty.PogoBreacher(pawn);

                        if (verb == null)
                        {
                            __instance.pawnsEscort.Add(pawn);
                        }
                        else if (!pawn.RaceProps.IsMechanoid && BreachingUtility.IsSoloAttackVerb(verb))
                        {
                            __instance.pawnSoloAttackers.Add(pawn);
                        }
                        else if (verb.verbProps.ai_IsBuildingDestroyer)
                        {
                            if (verb.IsMeleeAttack)
                            {
                                __instance.pawnsMeleeDestructive.Add(pawn);
                            }
                            else
                            {
                                __instance.pawnsRangedDestructive.Add(pawn);
                                __instance.Data.maxRange = Math.Min(__instance.Data.maxRange, verb.verbProps.range);
                            }
                        }
                        else if (verb.IsMeleeAttack)
                        {
                            __instance.pawnsEscort.Add(pawn);
                        }
                        else
                        {
                            __instance.pawnsRangedGeneral.Add(pawn);
                        }
                    }
                }

                bool flag = __instance.pawnsMeleeDestructive.Any<Pawn>();
                bool flag2 = __instance.pawnsRangedDestructive.Any<Pawn>();
                if (flag && (!flag2 || __instance.Data.preferMelee))
                {
                   RimWorld.LordToil_AssaultColonyBreaching.BalanceAndSetDuties(__instance.Data.breachDest, __instance.pawnsMeleeDestructive, __instance.pawnSoloAttackers, __instance.pawnsRangedDestructive, __instance.pawnsRangedGeneral, __instance.pawnsEscort);
                   RimWorld.LordToil_AssaultColonyBreaching.SetBackupDuty(__instance.pawnsLost);
                    return false;
                }
                if (flag2)
                {
                   RimWorld.LordToil_AssaultColonyBreaching.BalanceAndSetDuties(__instance.Data.breachDest, __instance.pawnsRangedDestructive, __instance.pawnSoloAttackers, __instance.pawnsRangedGeneral, __instance.pawnsMeleeDestructive, __instance.pawnsEscort);
                   RimWorld.LordToil_AssaultColonyBreaching.SetBackupDuty(__instance.pawnsLost);
                    return false;
                }
                if (__instance.pawnsRangedGeneral.Any<Pawn>())
                {
                   RimWorld.LordToil_AssaultColonyBreaching.BalanceAndSetDuties(__instance.Data.breachDest, __instance.pawnsRangedGeneral, __instance.pawnSoloAttackers, __instance.pawnsMeleeDestructive, __instance.pawnsRangedDestructive, __instance.pawnsEscort);
                   RimWorld.LordToil_AssaultColonyBreaching.SetBackupDuty(__instance.pawnsLost);
                    return false;
                }
               RimWorld.LordToil_AssaultColonyBreaching.SetBackupDuty(__instance.pawnsMeleeDestructive);
               RimWorld.LordToil_AssaultColonyBreaching.SetBackupDuty(__instance.pawnsRangedDestructive);
               RimWorld.LordToil_AssaultColonyBreaching.SetBackupDuty(__instance.pawnsRangedGeneral);
               RimWorld.LordToil_AssaultColonyBreaching.SetBackupDuty(__instance.pawnSoloAttackers);
               RimWorld.LordToil_AssaultColonyBreaching.SetBackupDuty(__instance.pawnsEscort);
               RimWorld.LordToil_AssaultColonyBreaching.SetBackupDuty(__instance.pawnsLost);
                return false;
            }

            public static void Postfix(RimWorld.LordToil_AssaultColonyBreaching __instance)
            {
                Log.Message($"pawnsMeleeDestructive {__instance.pawnsMeleeDestructive.Count}");
                Log.Message($"pawnsRangedDestructive {__instance.pawnsRangedDestructive.Count}");
                Log.Message($"pawnsRangedGeneral {__instance.pawnsRangedGeneral.Count}");
                Log.Message($"pawnSoloAttackers {__instance.pawnSoloAttackers.Count}");
                Log.Message($"pawnsEscort {__instance.pawnsEscort.Count}");
                Log.Message($"pawnsLost {__instance.pawnsLost.Count}");

                __instance.Data.maxRange = 144f;
            }
        }

        [HarmonyPatch(typeof(RimWorld.LordToil_AssaultColonyBreaching), "SetBreachDuty")]
        static class LordToil_AssaultColonyBreaching_SetBreachDuty
        {
            public static void Prefix(ref List<Pawn> breachers)
            {
                var pogoBreachers = new List<Pawn>();
                foreach (var breacher in breachers)
                {
                    if (PogoBreacher(breacher) != null)
                    {
                        pogoBreachers.Add(breacher);
                    }
                    else
                    {
                        breacher.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
                    }
                }
                breachers = pogoBreachers;
            }

            public static Verb PogoBreacher(Pawn pawn)
            {
                Pawn_EquipmentTracker equipment = pawn.equipment;
                CompEquippable compEquippable = (equipment != null) ? equipment.PrimaryEq : null;
                if (compEquippable == null)
                {
                    return null;
                }
                Verb primaryVerb = compEquippable.PrimaryVerb;
                if (BreachingUtility.UsableVerb(primaryVerb) && primaryVerb.verbProps.ai_IsBuildingDestroyer)
                {
                    return primaryVerb;
                }

                if (new string[] { "Stick", "Concussion", "HE", "Rocket", "Inferno", "Blast" }.Any(x => compEquippable.ToString().IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return primaryVerb;
                }

                return null;
            }


        }

        [HarmonyPatch(typeof(RimWorld.LordToil_AssaultColonyBreaching), "SetEscortDuty")]
        static class LordToil_AssaultColonyBreaching_SetEscortDuty
        {
            public static bool Prefix(List<Pawn> escorts)
            {
                for (int i = 0; i < escorts.Count; i++)
                {
                    Pawn pawn = escorts[i];
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(RimWorld.LordToil_AssaultColonyBreaching), "SetSoloAttackDuty")]
        static class LordToil_AssaultColonyBreaching_SetSoloAttackDuty
        {
            public static bool Prefix(List<Pawn> breachers, IntVec3 breachDest)
            {
                Log.Message($"breachers: {breachers.Count}");
                for (int i = 0; i < breachers.Count; i++)
                {
                    breachers[i].mindState.duty = new PawnDuty(DutyDefOf.Breaching, breachDest, -1f);
                }
                return false;
            }
        }
    }
}
