using HarmonyLib;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace PogoAI.Patches
{
    [HarmonyPatch(typeof(Verse.AI.CastPositionFinder), "TryFindCastPosition")]
    static class CastPositionFinder
    {
        public static void Postfix(CastPositionRequest newReq, ref bool __result)
        {
            var lord = newReq.caster?.GetLord();
            if (lord != null && lord.CurLordToil is RimWorld.LordToil_AssaultColonyBreaching lordToil)
            {
                if (!__result && lordToil.useAvoidGrid)
                {
                    lordToil.useAvoidGrid = false;
                    lordToil.Data.Reset();
#if DEBUG
                    Log.Message($"Couldnt find cast position so disabling avoid grid for breaching");
#endif
                }
            }
        }
    }
}
