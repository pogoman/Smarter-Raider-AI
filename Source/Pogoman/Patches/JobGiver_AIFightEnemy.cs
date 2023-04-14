using HarmonyLib;
using RimWorld;
using Verse.AI;
using Verse;

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
                if (__result != null && __result.targetA.Thing != null && __result.def == JobDefOf.AttackMelee)
                {
                    if (pawn.Position.DistanceTo(__result.targetA.Cell) > 3 && pawn.Map.avoidGrid.grid[pawn.Position] == 0 
                        && pawn.Map.avoidGrid.grid[__result.targetA.Thing.Position] > 0)
                    {
                        Job sapJob = null;
                        JobGiver_AISapper.JobGiver_AISapper_TryGiveJob_Patch.Prefix(pawn, ref sapJob);
                        if (sapJob?.def != JobDefOf.Follow)
                        {
                            __result = sapJob;
                        }
                    }
                }
            }
        }
    }
}
