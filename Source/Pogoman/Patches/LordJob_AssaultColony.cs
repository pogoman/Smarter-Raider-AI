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
                var instance = Traverse.Create(__instance);
                if (instance.Field("assaulterFaction").Property("def").Property("techLevel").GetValue<TechLevel>() >= Init.settings.minSmartTechLevel)
                {
                    instance.Field("breachers").SetValue(true);
                    instance.Field("useAvoidGridSmart").SetValue(!Init.combatAi);
                }
                BreachingUtility.breachMineables = false;
                BreachingUtility.enforceMinimumRange = true;
                BreachingUtility.doneReset = false;
                JobGiver_AISapper.pathCostCache.Clear();
                JobGiver_AISapper.findNewPaths = true;
            }
        }
    }
}
