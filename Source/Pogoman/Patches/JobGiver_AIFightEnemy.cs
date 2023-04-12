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
    internal class JobGiver_AIFightEnemy
    {
        [HarmonyPatch(typeof(RimWorld.JobGiver_AIFightEnemy), nameof(RimWorld.JobGiver_AIFightEnemy.TryGiveJob))]
        static class JobGiver_AIFightEnemy_TryGiveJob
        {
            static void Prefix(Pawn pawn, RimWorld.JobGiver_AIFightEnemy __instance)
            {
                __instance.needLOSToAcquireNonPawnTargets = true;
            }

            static void Postfix(Pawn pawn, ref Job __result)
            {
                if (__result != null && __result.def == JobDefOf.AttackMelee && 
                    __result?.targetA.Thing?.Position != null && (pawn.equipment?.PrimaryEq?.PrimaryVerb?.IsMeleeAttack ?? true))
                {
                    var target = __result.targetA.Thing.Position;
                    var sidesBlocked = Utilities.GetBlockedSides(pawn);                    
                    if (__result.def == JobDefOf.AttackMelee && sidesBlocked == 4 && !ReachabilityImmediate.CanReachImmediate(pawn.Position, target
                        , pawn.Map, PathEndMode.Touch, null))
                    {
                        __result = Utilities.GetTrashNearbyWallJob(pawn, 1);                        
                    }
                    if (__result != null)
                    {
                        __result.collideWithPawns = true;
                        __result.expiryInterval = 60;
                        __result.expireRequiresEnemiesNearby = false;
                        __result.ignoreDesignations = true;
                        __result.checkOverrideOnExpire = true;
                    }
                }                
            }
        }
    }
}
