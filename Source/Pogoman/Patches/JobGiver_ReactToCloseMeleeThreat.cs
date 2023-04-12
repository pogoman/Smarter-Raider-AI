using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using Unity.Jobs;
using static UnityEngine.GraphicsBuffer;
using Mono.Unix.Native;

namespace PogoAI.Patches
{
    internal class JobGiver_ReactToCloseMeleeThreat
    {
        [HarmonyPatch(typeof(RimWorld.JobGiver_ReactToCloseMeleeThreat), nameof(RimWorld.JobGiver_ReactToCloseMeleeThreat.TryGiveJob))]
        static class JobGiver_ReactToCloseMeleeThreat_TryGiveJob
        {
            static void Postfix(Pawn pawn, ref Job __result)
            {
                if (__result != null && __result.def == JobDefOf.AttackMelee && 
                    __result?.targetA.Thing?.Position != null && (pawn.equipment?.PrimaryEq?.PrimaryVerb?.IsMeleeAttack ?? true))
                {
                    var target = __result.targetA.Thing.Position;
                    var sidesBlocked = Utilities.GetBlockedSides(pawn);

#if DEBUG
                    if (target.DistanceTo(pawn.Position) <= 5)
                    {
                        Find.CurrentMap.debugDrawer.FlashCell(pawn.Position, 0.5f, $"SB:{sidesBlocked}", 60);
                    }
#endif

                    if (sidesBlocked > 0 && sidesBlocked < 4)
                    {
                        IntVec3 intVec = Utilities.GetNearestEmptyCell(pawn, 5);
                        if (intVec.IsValid)
                        {
                            __result = JobMaker.MakeJob(JobDefOf.Goto, intVec, 120, true);
                            __result.collideWithPawns = false;
                            __result.expireRequiresEnemiesNearby = false;
                            __result.ignoreDesignations = true;
                            __result.checkOverrideOnExpire = true;
                        }
                    }
                    else if (__result.def == JobDefOf.AttackMelee && sidesBlocked == 4 && !ReachabilityImmediate.CanReachImmediate(pawn.Position, target
                        , pawn.Map, PathEndMode.Touch, null))
                    {
                        __result = Utilities.GetTrashNearbyWallJob(pawn, 1);                        
                    }
                }
            }
        }
    }
}
