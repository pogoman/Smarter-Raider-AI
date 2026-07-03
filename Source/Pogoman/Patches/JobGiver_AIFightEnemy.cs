using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace PogoAI.Patches
{
    [HarmonyPatch(typeof(RimWorld.JobGiver_AIFightEnemy), "TryGiveJob")]
    public static class JobGiver_AIFightEnemy_TryGiveJob
    {
        static void Prefix(RimWorld.JobGiver_AIFightEnemy __instance)
        {
            __instance.needLOSToAcquireNonPawnTargets = true;
        }

        static void Postfix(Pawn pawn, ref Job __result)
        {
            if (__result != null && __result.targetA.Thing != null && __result.def == JobDefOf.AttackMelee)
            {
                __result.expiryInterval = Rand.RangeInclusive(Init.settings.reactionMin, Init.settings.reactionMax);
                var cellIndices = pawn.Map.cellIndices;
                var avoidGrid = pawn.Map.avoidGrid;
                // Drop the melee job if it means charging from a safe cell into defended ground,
                // or if the target cannot be reached at all.
                bool chargingIntoDefendedGround = pawn.Position.DistanceTo(__result.targetA.Cell) > 3
                    && avoidGrid.Grid[cellIndices.CellToIndex(pawn.Position)] == 0
                    && avoidGrid.Grid[cellIndices.CellToIndex(__result.targetA.Thing.Position)] > 0;
                if (chargingIntoDefendedGround || !pawn.CanReach(__result.targetA.Thing, PathEndMode.Touch, Danger.Deadly))
                {
                    __result = null;
                }
            }
        }
    }
}
