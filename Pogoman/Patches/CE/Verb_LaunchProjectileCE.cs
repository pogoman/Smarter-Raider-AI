using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Verse;
using Verse.AI;
using UnityEngine;

namespace PogoAI.Patches.CE
{
    internal class Verb_LaunchProjectileCE_Patch
    {
        [HarmonyPatch]
        static class Verb_LaunchProjectileCE_CanHitTargetFrom
        {
            static MethodBase target;

            static bool Prepare()
            {
                Init.CombatExtended = LoadedModManager.RunningMods.FirstOrDefault(m => m.Name == "Combat Extended");

                if (Init.CombatExtended == null)
                {
                    return false;
                }

                var assembly = Init.CombatExtended.assemblies.loadedAssemblies.FirstOrDefault(a => a.GetName().Name == "CombatExtended");

                var type = assembly   ?.GetType("CombatExtended.Verb_LaunchProjectileCE");

                if (type == null)
                {
                    Log.Warning("Can't patch CombatExtended. No Verb_LaunchProjectileCE");

                    return false;
                }

                target = AccessTools.DeclaredMethod(type, "CanHitTargetFrom", 
                    new Type[] { typeof(IntVec3), typeof(LocalTargetInfo)});

                if (target == null)
                {
                    Log.Warning("Can't patch Verb_LaunchProjectileCE. No CanHitTargetFrom");

                    return false;
                }

                //var original = typeof(Verb).GetMethod("TryFindShootLineFromTo");
                //Init.harm.Unpatch(original, HarmonyPatchType.Prefix, "CombatExtended.HarmonyCE");

                return true;
            }

            static MethodBase TargetMethod()
            {
                return target;
            }

            //static bool Prefix(IntVec3 root, LocalTargetInfo targ, Verb __instance, ref bool __result)
            //{
            //    if (targ.Thing != null && targ.Thing == __instance.caster)
            //    {
            //        __result = __instance.targetParams.canTargetSelf;
            //        return false;
            //    }
            //    ShootLine shootLine;
            //    __result = !__instance.ApparelPreventsShooting() && __instance.TryFindShootLineFromTo(root, targ, out shootLine);
            //    return false;
            //}

        }
    }
}
