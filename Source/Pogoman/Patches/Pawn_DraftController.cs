using HarmonyLib;

namespace PogoAI.Patches
{
    internal class Pawn_DraftController
    {

        [HarmonyPatch(typeof(RimWorld.Pawn_DraftController), "Drafted", MethodType.Setter)]
        static class Pawn_DraftController_Drafted_Set
        {
            static void Postfix(RimWorld.Pawn_DraftController __instance)
            {
                __instance.pawn.Map.avoidGrid.gridDirty = true;
            }
        }
    }
}
