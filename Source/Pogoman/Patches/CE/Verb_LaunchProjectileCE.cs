using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Verse;
using Verse.AI;
using UnityEngine;
using PogoAI.Extensions;

namespace PogoAI.Patches.CE
{
    internal class Verb_LaunchProjectileCE_Patch
    {
        [HarmonyPatch]
        static class Verb_LaunchProjectileCE_TryFindCEShootLineFromTo
        {
            static MethodBase target;
            static ModContentPack CE;

            static bool Prepare()
            {
                CE = LoadedModManager.RunningMods.FirstOrDefault(m => m.Name == "Combat Extended");

                if (CE == null || !Init.settings.CombatExtendedCompatPerf)
                {
                    return false;
                }

                var assembly = CE.assemblies.loadedAssemblies.FirstOrDefault(a => a.GetName().Name == "CombatExtended");

                var type = assembly?.GetType("CombatExtended.Verb_LaunchProjectileCE");

                if (type == null)
                {
                    Log.Warning("Can't patch CombatExtended. No Verb_LaunchProjectileCE");

                    return false;
                }

                target = AccessTools.DeclaredMethod(type, "TryFindCEShootLineFromTo", 
                    new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(ShootLine).MakeByRefType() });

                if (target == null)
                {
                    Log.Warning("Can't patch Verb_LaunchProjectileCE. No TryFindCEShootLineFromTo");

                    return false;
                }

                var original = typeof(Verb).GetMethod("TryFindShootLineFromTo");
                Init.harm.Unpatch(original, HarmonyPatchType.Prefix, "CombatExtended.HarmonyCE");

                return true;
            }

            static MethodBase TargetMethod()
            {
                return target;
            }

            static bool Prefix(IntVec3 root, LocalTargetInfo targ, ref ShootLine resultingLine, Verb __instance, ref bool __result)
            {
                if (targ.Pawn != null || (__instance.CasterPawn?.IsColonist ?? false) || (__instance.EquipmentSource?.def?.ToString().Matches("Mortar") ?? false)) {
                    return true;
                }
                __result = __instance.TryFindShootLineFromTo(root, targ, out resultingLine);
                return false;
            }

        }
    }
}
