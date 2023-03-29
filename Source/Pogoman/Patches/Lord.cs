using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PogoAI.Patches
{
    internal class Lord
    {
        [HarmonyPatch(typeof(Verse.AI.Group.Lord), "LordTick")]
        static class Lord_LordTick
        {
            static void Postfix(Verse.AI.Group.Lord __instance)
            {
                if (__instance.ticksInToil % 300 == 0)
                {
                    __instance.Map.avoidGrid.gridDirty = true;
                }
            }
        }
    }
}
