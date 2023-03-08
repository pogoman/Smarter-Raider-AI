using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace PogoAI.Patches
{
    internal class Pawn_JobTracker
    {
        [HarmonyPatch(typeof(Verse.AI.Pawn_JobTracker), "StartJob")]
        static class Pawn_JobTracker_StartJob
        {
            //public static bool Prefix(Verse.AI.Pawn_JobTracker __instance, Job newJob)
            //{
            //    if (newJob.ToString() == __instance.curJob?.ToString())
            //    {
            //        Log.Message("Dupe job quitting");
            //    }
            //    return newJob.ToString() != __instance.curJob?.ToString(); 
            //}
        }
    }
}
