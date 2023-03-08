using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace PogoAI.Patches
{
    internal class RaidStrategyWorker_ImmediateAttack
    {
        [HarmonyPatch(typeof(RimWorld.RaidStrategyWorker_ImmediateAttack), "MakeLordJob")]
        internal static class RaidStrategyWorker_ImmediateAttack_MakeLordJob
        {
            public static void Postfix(ref LordJob __result)
            {
                if (__result is LordJob_AssaultColony)
                {
                    Log.Message("Breachers");
                    ((LordJob_AssaultColony)__result).useAvoidGridSmart = true;
                    ((LordJob_AssaultColony)__result).breachers = true;
                }
            }
        }
    }
}
