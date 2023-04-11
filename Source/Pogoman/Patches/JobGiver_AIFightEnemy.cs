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
                if (__result?.targetA.Thing?.Position != null && (pawn.equipment?.PrimaryEq?.PrimaryVerb?.IsMeleeAttack ?? true))
                {
#if DEBUG
                    if (__result != null)
                    {
                        Find.CurrentMap.debugDrawer.FlashCell(pawn.Position, 0.8f, $"Fight", 60);
                    }
#endif
                    using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, __result.targetA.Thing.Position,
                            TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false, false, false), PathEndMode.Touch, null))
                    {
                        if (pawnPath == PawnPath.NotFound || pawnPath.nodes.Any(x => PawnUtility.AnyPawnBlockingPathAt(x, pawn, true, false, false)))
                        {
                            __result = Utilities.GetTrashNearbyWallJob(pawn, 10);
                            if (__result == null)
                            {
                                IntVec3 intVec = CellFinder.RandomClosewalkCellNear(pawn.Position, pawn.Map, 10, null);
                                __result = JobMaker.MakeJob(JobDefOf.Goto, intVec, 500, true);
                            }
                            if (__result != null)
                            {
                                __result.collideWithPawns = true;
                                __result.expiryInterval = 120;
                                __result.expireRequiresEnemiesNearby = false;
                                __result.ignoreDesignations = true;
                                __result.checkOverrideOnExpire = true;
                            }
                        }
                    }
                }
            }
        }
    }
}
