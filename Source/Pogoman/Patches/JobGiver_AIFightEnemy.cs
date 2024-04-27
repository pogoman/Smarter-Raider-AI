using HarmonyLib;
using RimWorld;
using Verse.AI;
using Verse;

namespace PogoAI.Patches
{
    public class JobGiver_AIFightEnemy
    {
        public static class JobGiver_AIFightEnemy_TryGiveJob
        {
            static void Prefix(Pawn pawn, RimWorld.JobGiver_AIFightEnemy __instance)
            {
                Traverse.Create(__instance).Field("needLOSToAcquireNonPawnTargets").SetValue(true);
            }

            static void Postfix(Pawn pawn, ref Job __result)
            {
                if (__result != null && __result.targetA.Thing != null && __result.def == JobDefOf.AttackMelee)
                {
                    if (pawn.Position.DistanceTo(__result.targetA.Cell) > 3 && pawn.Map.avoidGrid.Grid[pawn.Position] == 0 
                        && pawn.Map.avoidGrid.Grid[__result.targetA.Thing.Position] > 0 || !pawn.CanReach(__result.targetA.Thing, PathEndMode.Touch, Danger.Deadly))
                    {
                        __result = null;
                    }
                }
            }
        }
    }
}
