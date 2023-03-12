using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PogoAI.Patches
{
    internal class LordToil_AssaultColonyBreaching
    {
        [HarmonyPatch(typeof(RimWorld.LordToil_AssaultColonyBreaching), "UpdateAllDuties")]
        static class LordToil_AssaultColonyBreaching_UpdateAllDuties
        {
            static void Postfix(RimWorld.LordToil_AssaultColonyBreaching __instance)
            {
                __instance.Data.maxRange = 40f;
            }

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
                    var target = __instance.Map.attackTargetsCache.TargetsHostileToFaction(checkWith.Faction).First();
                    using (PawnPath breachPath = __instance.Map.pathFinder.FindPath(checkWith.Position, target.Thing.Position,
                        TraverseParms.For(checkWith, Danger.Deadly, TraverseMode.PassAllDestroyableThings, false, true, false), PathEndMode.OnCell, null))
                    {
                        using (PawnPath pathNoBreach = __instance.Map.pathFinder.FindPath(checkWith.Position, target.Thing.Position,
                        TraverseParms.For(checkWith, Danger.Deadly, TraverseMode.ByPawn, false, true, false), PathEndMode.OnCell, null))
                        {
                            //Log.Message($"nobreac: {breachPath.TotalCost} walk cost {pathNoBreach.TotalCost}");
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
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
        }
    }
}
