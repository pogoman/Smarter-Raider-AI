using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using PogoAI.Extensions;

namespace PogoAI
{
    public static class Utilities
    {
        public static Job GetTrashNearbyWallJob(Pawn pawn, int radius)
        {
            Job job = null;
            Building trashTarget = null;
            for (int i = 0; i < GenRadial.NumCellsInRadius(radius); i++)
            {
                IntVec3 c = pawn.Position + GenRadial.RadialPattern[i];
                if (c.InBounds(pawn.Map))
                {
                    var edifice = c.GetEdifice(pawn.Map);
                    if (edifice != null && (edifice.def == ThingDefOf.Wall || edifice.def == ThingDefOf.Door || edifice.def.defName.Matches("embrasure") 
                        || (edifice.def.mineable && edifice.def != ThingDefOf.CollapsedRocks && edifice.def != ThingDefOf.RaisedRocks))
                        && pawn.CanReserve(edifice))
                    {
                        using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, edifice,
                            TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false, false, false), PathEndMode.Touch, null))
                        {
                            if (pawnPath == PawnPath.NotFound || pawnPath.nodes.Any(x => PawnUtility.AnyPawnBlockingPathAt(x, pawn, true, false, false)))
                            {
                                continue;
                            }
                            else
                            {
                                trashTarget = edifice;
                                break;
                            }
                        }
                    }
                }
            }
            if (trashTarget != null)
            {
                #if DEBUG
                    Find.CurrentMap.debugDrawer.FlashCell(trashTarget.Position, 0.6f, $"smash", 120);
#endif
                var canMineMineables = true;
                if (StatDefOf.MiningSpeed.Worker.IsDisabledFor(pawn))
                {
                    canMineMineables = false;
                }
                if (trashTarget.def.mineable)
                {
                    if (canMineMineables)
                    {
                        job = JobMaker.MakeJob(JobDefOf.Mine, trashTarget);
                    }
                    else
                    {
                        job = JobMaker.MakeJob(JobDefOf.AttackMelee, trashTarget);
                    }
                }
                else
                {
                    job = TrashUtility.TrashJob(pawn, trashTarget, true, false);
                }
                if (job != null)
                {
                    job.expireRequiresEnemiesNearby = false;
                    job.expiryInterval = 120;
                    job.collideWithPawns = true;
                }
            }
            return job;
        }

        public static Thing GetNearbyThing(Pawn pawn, ThingDef thingDef, int radius)
        {
            Building building = null;
            for (int i = 0; i < GenRadial.NumCellsInRadius(radius); i++)
            {
                IntVec3 c = pawn.Position + GenRadial.RadialPattern[i];
                if (c.InBounds(pawn.Map))
                {
                    var edifice = c.GetEdifice(pawn.Map);
                    if (edifice != null && edifice.def == thingDef && edifice.HitPoints > 0)
                    {
                        building = edifice;
                        break;
                    }
                }
            }
            return building;
        }
    }
}
