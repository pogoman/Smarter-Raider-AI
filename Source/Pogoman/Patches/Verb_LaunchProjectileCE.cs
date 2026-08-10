using HarmonyLib;
using PogoAI.Extensions;
using System;
using System.Linq;
using System.Reflection;
using Verse;

namespace PogoAI.Patches
{
    internal class Verb_LaunchProjectileCE_Patch
    {
        [HarmonyPatch]
        static class Verb_LaunchProjectileCE_TryFindCEShootLineFromTo
        {
            static MethodBase target;
            static ModContentPack CE;

            static bool Prefix(IntVec3 root, LocalTargetInfo targ, ref ShootLine resultingLine, Verb __instance, ref bool __result)
            {
                if (__instance.EquipmentSource?.def?.ToString().Matches("Mortar") ?? false)
                {
                    return true;
                }
                __result = __instance.TryFindShootLineFromTo(root, targ, out resultingLine);
                if (__result)
                {
                    return true;
                }
                return false;
            }
        }
    }
}
