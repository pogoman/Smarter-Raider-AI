using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace PogoAI.Patches
{
    internal class LordJob_StageThenAttack
    {
        [HarmonyPatch(typeof(RimWorld.LordJob_StageThenAttack), "CreateGraph")]
        static class LordJob_StageThenAttack_CreateGraph
        {
            static void Postfix(RimWorld.LordJob_StageThenAttack __instance, ref StateGraph __result)
            {
                foreach (var transition in __result.transitions)
                {
                    if (transition.target is RimWorld.LordToil_AssaultColonyBreaching)
                    {
                        transition.AddTrigger(new Trigger_PawnHarmed());
                    }
                }
            }
        }
    }
}
