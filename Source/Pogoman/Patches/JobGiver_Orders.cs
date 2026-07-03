using HarmonyLib;
using Verse;

namespace PogoAI.Patches
{
    internal class JobGiver_Orders
    {

        [HarmonyPatch(typeof(Verse.AI.JobGiver_Orders), "TryGiveJob")]
        static class JobGiver_Orders_TryGiveJob_Patch
        {
            static void Postfix(Pawn pawn)
            {
                if (pawn.Drafted && pawn.Map != null)
                {
                    pawn.Map.avoidGrid.gridDirty = true;
                }
            }
        }
    }
}
