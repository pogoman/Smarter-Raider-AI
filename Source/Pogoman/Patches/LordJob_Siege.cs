using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace PogoAI.Patches
{
    internal class LordJob_Siege
    {
        [HarmonyPatch(typeof(RimWorld.LordJob_Siege), "CreateGraph")]
        static class LordJob_Siege_CreateGraph
        {
            static void Postfix(RimWorld.LordJob_Siege __instance, ref StateGraph __result)
            {
                foreach (var transition in __result.transitions)
                {
                    Log.Message($"{transition.target}");
                    if (transition.target is RimWorld.LordToil_AssaultColonyBreaching)
                    {
                        transition.triggers.RemoveAll(t => t is Trigger_PawnHarmed);
                    }
                }
            }
        }
    }
}
