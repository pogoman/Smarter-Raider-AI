using HarmonyLib;
using RimWorld;
using Verse.AI;
using Verse;
using Mono.Unix.Native;

namespace PogoAI.Patches
{
    internal class JobGiver_AITrashColonyClose
    {
        [HarmonyPatch(typeof(RimWorld.JobGiver_AITrashColonyClose), nameof(RimWorld.JobGiver_AITrashColonyClose.TryGiveJob))]
        static class JobGiver_AITrashColonyClose_TryGiveJob
        {
            static void Postfix(Pawn pawn, ref Job __result)
            {
                if (__result != null && __result.def == JobDefOf.AttackMelee && 
                    __result?.targetA.Thing?.Position != null)
                {
                    var target = __result.targetA.Thing.Position;
                    if (ReachabilityImmediate.CanReachImmediate(pawn.Position, target, pawn.Map, PathEndMode.Touch, null))
                    {
                        Find.CurrentMap.debugDrawer.FlashCell(pawn.Position, 0.1f, $"CC", 60);
                        Utilities.MaybeMoveOutTheWayJob(pawn, target, ref __result);
                    }
                }
            }
        }
    }
}
