using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PogoAI.Patches
{
    [HarmonyPatch(typeof(Verse.AI.Group.Lord), "LordTick")]
    static class Lord_LordTick
    {
        static int lastUpdateTicks = 0;

        static void Postfix(Verse.AI.Group.Lord __instance)
        {
            if ((Find.TickManager.TicksGame - lastUpdateTicks) / 60 > 5)
            {
                Traverse.Create(__instance.Map.avoidGrid).Field("gridDirty").SetValue(true);
                lastUpdateTicks = Find.TickManager.TicksGame;
            }
        }
    }
}
