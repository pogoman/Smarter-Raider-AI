using HarmonyLib;
using RimWorld;
using System;

namespace PogoAI.Patches
{
    internal class LordJob_AssaultColony
    {
        [HarmonyPatch(typeof(RimWorld.LordJob_AssaultColony), MethodType.Constructor, new Type[] { typeof(Faction), 
            typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
        static class LordJob_AssaultColony_Ctor
        {
            static void Postfix(ref RimWorld.LordJob_AssaultColony __instance)
            {
                __instance.breachers = !Init.CAI;
                __instance.useAvoidGridSmart = true;
                BreachingUtility.breachMineables = false;
                JobGiver_AISapper.pathCostCache.Clear();
                JobGiver_AISapper.findNewPaths = true;
            }
        }
    }
}
