using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using PogoAI.Extensions;
using static UnityEngine.GraphicsBuffer;

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

        public static void MaybeMoveOutTheWayJob(Pawn pawn, IntVec3 target, ref Job __result)
        {
            if (PawnBlocked(pawn, IntVec3.Invalid))
            {
                //Cant move anyway
                return;
            }
            if (GetNearestBlockingThingsCategory(pawn, 1, IntVec3.Invalid).Where(x => x.Faction == pawn.Faction).Any(x => PawnBlocked((Pawn)x, x.Position + x.Rotation.Opposite.FacingCell)))
            {
                IntVec3 intVec = GetNearestEmptyCell(pawn, 5, target);
                if (intVec.IsValid)
                {
                    __result = JobMaker.MakeJob(JobDefOf.Goto, intVec, 120, true);
                    __result.collideWithPawns = true;
                    __result.expireRequiresEnemiesNearby = false;
                    __result.ignoreDesignations = true;
                    __result.checkOverrideOnExpire = true;
#if DEBUG
                    Find.CurrentMap.debugDrawer.FlashCell(pawn.Position, 0.5f, $"UB:{intVec}", 60);
#endif
                }
            }
        }

        public static bool PawnBlocked(Thing thing, IntVec3 ignoreCell)
        {
            return CountSurroundingImpassable(thing, 1, ignoreCell) + GetNearestBlockingThingsCategory(thing, 1, ignoreCell)
                .Count(x => x.Faction == thing.Faction) == (!ignoreCell.IsValid ? 4 : 3);
        }

        public static int CountSurroundingImpassable(Thing thing, int radius, IntVec3 ignoreCell)
        {
            var count = 0;
            for (int i = 1; i < GenRadial.NumCellsInRadius(radius); i++)
            {
                IntVec3 c = thing.Position + GenRadial.RadialPattern[i];
                if (c.InBounds(thing.Map) && c != ignoreCell)
                {
                    var edifice = c.GetEdifice(thing.Map);
                    if (edifice != null && edifice.GetRegion(RegionType.Set_Passable) == null)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public static List<Thing> GetNearestBlockingThingsCategory(Thing thing, int radius, IntVec3 ignoreCell)
        {
            var things = new List<Thing>();
            for (int i = 1; i < GenRadial.NumCellsInRadius(radius); i++)
            {
                IntVec3 c = thing.Position + GenRadial.RadialPattern[i];
                if (c.InBounds(thing.Map) && c != ignoreCell)
                {
                    Thing blockingThing = thing.Map.thingGrid.ThingsAt(c).FirstOrDefault(x => x is Building ||  (x is Pawn && x.Faction == thing.Faction) || x.BlocksPawn(thing as Pawn));
                    if (blockingThing != null)
                    {
#if DEBUG
                        Find.CurrentMap.debugDrawer.FlashCell(c, 0.1f, $"blocks", 60);
#endif
                        things.Add(blockingThing);
                    }
                    else
                    {
#if DEBUG
                        Find.CurrentMap.debugDrawer.FlashCell(c, 0.5f, $"", 60);
#endif
                    }
                }
            }
            return things;
        }

        public static Thing GetNearestThingDef(Pawn pawn, ThingDef thingDef, int radius)
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

        public static IntVec3 GetNearestEmptyCell(Pawn pawn, int radius, IntVec3 position)
        {
            IntVec3 intVec = IntVec3.Invalid;
            for (int i = 1; i < GenRadial.NumCellsInRadius(radius); i++)
            {
                intVec = position + GenRadial.RadialPattern[i];
                if (intVec != pawn.Position && intVec.InBounds(pawn.Map))
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
