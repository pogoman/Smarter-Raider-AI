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
    internal class JobGiver_AITrashBuildingsDistant
    {
        [HarmonyPatch(typeof(RimWorld.JobGiver_AITrashBuildingsDistant), "TryGiveJob")]
        static class JobGiver_AITrashBuildingsDistant_TryGiveJob_Patch
        {
            static bool Prefix(ref RimWorld.JobGiver_AITrashBuildingsDistant __instance, Pawn pawn, ref Job __result)
            {
                //Always sap first
                Job sapJob = null;
                JobGiver_AISapper.JobGiver_AISapper_TryGiveJob_Patch.Prefix(pawn, ref sapJob);
                if (sapJob != null && sapJob.def != JobDefOf.Wait && (sapJob.def != JobDefOf.Goto || sapJob.expiryInterval == 501))
                {
                    __result = sapJob;
                    return false;
                }
                __instance.attackAllInert = true;
                var allBuildingsColonist = pawn.Map.listerBuildings.allBuildingsColonist.Where(x =>
                    !x.def.IsFrame && x.HitPoints > 0 && x.def.altitudeLayer != AltitudeLayer.Conduits && x.def.designationCategory != DesignationCategoryDefOf.Security
                    && !x.IsBurning() && x.Position.DistanceTo(pawn.Position) < 20 && GenSight.LineOfSight(pawn.Position, x.Position, x.Map)).InRandomOrder();
                foreach (var building in allBuildingsColonist)
                {
                    if (TrashUtility.ShouldTrashBuilding(pawn, building, __instance.attackAllInert))
                    {
                        using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, building.Position,
                            TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false, false, false), PathEndMode.Touch, null))
                        {
                            if (pawnPath.NodesLeftCount > 1 && PawnUtility.AnyPawnBlockingPathAt(pawnPath.nodes[0], pawn, true, false, false))
                            {
                                continue;
                            }
                        }
                        Job job = TrashUtility.TrashJob(pawn, building, __instance.attackAllInert, false);
                        if (job != null)
                        {
                            job.expireRequiresEnemiesNearby = false;
                            job.expiryInterval = 120;
                            __result = job;
                            break;
                        }
                    }
                }
                if (__result == null)
                {
                    __result = sapJob;
                }
                return false;
            }
        }
    }
}
