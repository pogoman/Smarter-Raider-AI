using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace PogoAI.Patches
{
    internal class JobGiver_AIFightEnemy
    {
        [HarmonyPatch(typeof(RimWorld.JobGiver_AIFightEnemy), nameof(RimWorld.JobGiver_AIFightEnemy.TryGiveJob))]
        static class JobGiver_AIFightEnemy_TryGiveJob
        {
            static void Prefix(RimWorld.JobGiver_AIFightEnemy __instance)
            {
                __instance.needLOSToAcquireNonPawnTargets = true;
            }

            static void Postfix(Pawn pawn, ref Job __result)
            {
                if (__result?.targetA.Thing?.Position != null && (pawn.equipment.PrimaryEq?.PrimaryVerb?.IsMeleeAttack ?? true))
                {
                    using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, __result.targetA.Thing.Position,
                        TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false, false, false), PathEndMode.Touch, null))
                    {
                        if (pawnPath.NodesLeftCount > 1 && PawnUtility.AnyPawnBlockingPathAt(pawnPath.nodes[0], pawn, true, false, false))
                        {
                            __result = null;
                        }
                    }
                }
            }
        }
    }
}
