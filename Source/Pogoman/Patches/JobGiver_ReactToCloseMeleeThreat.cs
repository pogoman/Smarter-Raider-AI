using HarmonyLib;
using RimWorld;
using Verse.AI;
using Verse;

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
                    Utilities.MaybeMoveOutTheWayJob(pawn, target, ref __result);
                }
            }
        }
    }
}
