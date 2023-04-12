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
        /// <summary>
        /// 1 - 4
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static int GetBlockedSides(Pawn pawn)
        {
            var frontCell = pawn.Position + pawn.Rotation.FacingCell;
            var frontEdifice = frontCell.GetEdifice(pawn.Map);
            var frontBlocked = (frontEdifice == null ? false : frontEdifice.GetRegion(RegionType.Set_Passable) == null) || PawnUtility.AnyPawnBlockingPathAt(frontCell, pawn, true, false, false);
            var behindCell = pawn.Position + pawn.Rotation.Opposite.FacingCell;
            var behindEdifice = behindCell.GetEdifice(pawn.Map);
            var behindBlocked = (behindEdifice == null ? false : behindEdifice.GetRegion(RegionType.Set_Passable) == null) || PawnUtility.AnyPawnBlockingPathAt(behindCell, pawn, true, false, false);
            var rightCell = pawn.Position + pawn.Rotation.RighthandCell;
            var rightEdifice = rightCell.GetEdifice(pawn.Map);
            var rightBlocked = (rightEdifice == null ? false : rightEdifice.GetRegion(RegionType.Set_Passable) == null) || PawnUtility.AnyPawnBlockingPathAt(rightCell, pawn, true, false, false);
            var leftCell = pawn.Position - pawn.Rotation.RighthandCell;
            var leftEdifice = leftCell.GetEdifice(pawn.Map);
            var leftBlocked = (leftEdifice == null ? false : leftEdifice.GetRegion(RegionType.Set_Passable) == null) || PawnUtility.AnyPawnBlockingPathAt(leftCell, pawn, true, false, false);
            return new bool[] { frontBlocked, behindBlocked, leftBlocked, rightBlocked }.Count(x => x);
        }

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

        public static Thing GetNearestThing(Pawn pawn, ThingDef thingDef, int radius)
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

        public static IntVec3 GetNearestEmptyCell(Pawn pawn, int radius)
        {
            IntVec3 intVec = IntVec3.Invalid;
            for (int i = 1; i < GenRadial.NumCellsInRadius(radius); i++)
            {
                intVec = pawn.Position + GenRadial.RadialPattern[i];
                if (intVec.InBounds(pawn.Map))
                {
                    var edifice = intVec.GetEdifice(pawn.Map);
                    if ((edifice == null || edifice.GetRegion(RegionType.Set_Passable) != null) && !PawnUtility.AnyPawnBlockingPathAt(intVec, pawn))
                    {
                        break;
                    }
                }
            }
            return intVec;
        }
    }
}
