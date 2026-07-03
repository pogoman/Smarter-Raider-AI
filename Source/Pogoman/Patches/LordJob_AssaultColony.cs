using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace PogoAI.Patches
{
    internal class LordJob_AssaultColony
    {
        [HarmonyPatch(typeof(RimWorld.LordJob_AssaultColony), MethodType.Constructor, new Type[] { typeof(Faction),
            typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
        static class LordJob_AssaultColony_Ctor
        {
            static void Postfix(RimWorld.LordJob_AssaultColony __instance)
            {
                if (__instance.assaulterFaction?.def.techLevel >= Init.settings.minSmartTechLevel)
                {
                    __instance.breachers = true;
                    __instance.useAvoidGridSmart = !Init.combatAi;
                }
                Utilities.RestoreBuildingDestroyerFlags();
                if (Current.Game != null)
                {
                    foreach (var map in Find.Maps)
                    {
                        var comp = PogoMapComponent.For(map);
                        comp?.ResetBreachState();
                        comp?.ResetSapperState();
                    }
                }
            }
        }
    }
}
