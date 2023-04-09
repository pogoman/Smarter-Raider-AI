using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using Verse.Noise;
using PogoAI.Extensions;
using Mono.Unix.Native;

namespace PogoAI.Patches
{
    internal class JobGiver_AITrashBuildingsDistant
    {
        [HarmonyPatch(typeof(RimWorld.JobGiver_AITrashBuildingsDistant), "TryGiveJob")]
        static class JobGiver_AITrashBuildingsDistant_TryGiveJob_Patch
        {
            private static bool IsRoomWall(Building wall)
            {
                var intVec = wall.Position;
                Building edifice = intVec.GetEdifice(Find.CurrentMap);
                if (edifice != null)
                {
                    foreach (IntVec3 intVec3 in edifice.OccupiedRect().ExpandedBy(1).ClipInsideMap(Find.CurrentMap))
                    {
                        var room = intVec3.GetRoom(Find.CurrentMap);
                        if (room != null && !room.PsychologicallyOutdoors)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            static bool Prefix(ref RimWorld.JobGiver_AITrashBuildingsDistant __instance, Pawn pawn, ref Job __result)
            {
                //Ignore for insects
                if (pawn.Faction == Faction.OfInsects)
                {
                    return true;
                }
                //Otherwise always sap first
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
                    && !x.IsBurning() && pawn.Position.DistanceTo(x.Position) < 20
                    && GenSight.LineOfSight(pawn.Position, x.Position, x.Map)).InRandomOrder().ToList();

                var nonStructures = allBuildingsColonist.Count(x => x.def.designationCategory != DesignationCategoryDefOf.Structure);

                if (nonStructures == 0)
                {
                    return false;
                }

                foreach (var building in allBuildingsColonist)
                {
                    if (!pawn.CanReach(building.Position, PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn))
                    {
                        continue;
                    }
                    if (building.def.designationCategory == DesignationCategoryDefOf.Structure)
                    {
                        //if (!IsRoomWall(building) && !building.CanBeSeenOver())
                        //{
                        //    continue;
                        //}
                        using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, building.Position,
                            TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false, false, false), PathEndMode.Touch, null))
                        {
                            if (pawnPath.NodesLeftCount > 1 && pawnPath.nodes.Any(x => PawnUtility.AnyPawnBlockingPathAt(x, pawn, true, false, false)))
                            {
                                continue;
                            }
                        }
                    }
                    Job job = TrashUtility.TrashJob(pawn, building, __instance.attackAllInert, false);
                    if (job != null)
                    {
                        job.expireRequiresEnemiesNearby = false;
                        job.expiryInterval = 120;
                        job.collideWithPawns = true;
                        __result = job;
                        break;
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
