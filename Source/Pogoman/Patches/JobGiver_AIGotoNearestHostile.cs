using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using RimWorld;
using Verse.AI.Group;
using UnityEngine.Analytics;
using Verse.Noise;
using Mono.Unix.Native;
using Unity.Jobs;
using static UnityEngine.GraphicsBuffer;
using UnityEngine;
using static Verse.AI.BreachingGrid;

namespace PogoAI.Patches
{
    internal class JobGiver_AIGotoNearestHostile
    {
        [HarmonyPatch(typeof(RimWorld.JobGiver_AIGotoNearestHostile), "TryGiveJob")]
        static class JobGiver_AIGotoNearestHostile_TryGiveJob_Patch
        {
            static bool Prefix()
            {
                return false;
            }
        }
    }
}
