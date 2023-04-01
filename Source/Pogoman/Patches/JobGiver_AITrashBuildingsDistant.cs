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
                List<Building> allBuildingsColonist = pawn.Map.listerBuildings.allBuildingsColonist.Where(x =>
                    !x.def.IsFrame && x.HitPoints > 0 && x.def.altitudeLayer != AltitudeLayer.Conduits && x.def.designationCategory != DesignationCategoryDefOf.Security
                    && !x.IsBurning() && x.Position.DistanceTo(pawn.Position) < 10 && GenSight.LineOfSight(pawn.Position, x.Position, x.Map))
                    .OrderBy(x => x.Position.DistanceTo(pawn.Position)).ToList();
                foreach (var building in allBuildingsColonist)
                {
                    if (TrashUtility.ShouldTrashBuilding(pawn, building, __instance.attackAllInert))
                    {
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
