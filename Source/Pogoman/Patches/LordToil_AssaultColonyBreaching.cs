using HarmonyLib;
using PogoAI.Extensions;
using System.Linq;
using Verse;

namespace PogoAI.Patches
{
    internal class LordToil_AssaultColonyBreaching
    {
        [HarmonyPatch(typeof(RimWorld.LordToil_AssaultColonyBreaching), "UpdateAllDuties")]
        static class LordToil_AssaultColonyBreaching_UpdateAllDuties
        {
            static void Postfix(RimWorld.LordToil_AssaultColonyBreaching __instance)
            {
                __instance.Data.maxRange = 75f;
            }

            static bool Prefix(RimWorld.LordToil_AssaultColonyBreaching __instance)
            {
                if (__instance.useAvoidGrid && __instance.lord.ownedPawns.Any(x => x.def.ToString().Matches("centipede")))
                {
                    __instance.useAvoidGrid = false;
                }

                return true;
            }
        }
    }
}
