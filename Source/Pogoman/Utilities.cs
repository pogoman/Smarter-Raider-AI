using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using Verse;
using PogoAI.Extensions;

namespace PogoAI
{
    public static class Utilities
    {
        public static bool RoomIsBreached(Pawn pawn, IntVec3 roomCell)
        {
            var room = roomCell.GetRoom(Find.CurrentMap);
            return (room != null && room.PsychologicallyOutdoors) || (room == null && pawn.CanReach(roomCell, PathEndMode.Touch, Danger.Deadly));
        }


        public static bool CellBlockedFor(Thing thing, IntVec3 cell)
        {
            Building edifice = cell.GetEdifice(thing.Map);
            if (edifice != null)
            {
                Building_Door building_Door = edifice as Building_Door;
                var flag = false;
                var flag2 = false;
                if (thing is Pawn)
                {
                    var pawn = thing as Pawn; 
                    flag = building_Door != null && !building_Door.FreePassage && !building_Door.PawnCanOpen(pawn);
                    flag2 = edifice.def.IsFence && !pawn.def.race.CanPassFences;
                }                
                if (flag || flag2 || edifice.def.passability == Traversability.Impassable)
                {
                    return true;
                }
            }
            return false;
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

        public static void MaybeMoveOutTheWayJob(Pawn pawn, ref Job __result, Thing target = null)
        {
            IntVec3 intVec = GetNearestEmptyCell(pawn, 1, target?.Position ?? pawn.Position);
            if (!intVec.IsValid)
            {
                return; //Cant move anyway                
            }
            if (GetPawnsInRadius(pawn.Map, pawn.Position, 1, IntVec3.Invalid).Where(x => x.Faction == pawn.Faction)
                .Any(x => ThingBlocked((Pawn)x, x.Position + x.Rotation.Opposite.FacingCell)))
            {
#if DEBUG
                //Log.Message($"{pawn} {pawn.Position} moving out way to {intVec}");
#endif
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

        public static bool ThingBlocked(Thing thing, IntVec3 ignoreCell, bool includeCorners = false)
        {
            var count = CountSurroundingImpassable(thing, 1, ignoreCell, includeCorners) + GetPawnsInRadius(thing.Map, thing.Position, 1, ignoreCell, includeCorners).Count;
#if DEBUG
            Find.CurrentMap.debugDrawer.FlashCell(thing.Position, 1f, $"PB:{count}", 60); 
#endif
            return count == ((!ignoreCell.IsValid ? 4 : 3) + 4 * (includeCorners ? 1 : 0));
        }

        public static int CountSurroundingImpassable(Thing thing, int radius, IntVec3 ignoreCell, bool includeCorners = false)
        {
            var count = 0;
            for (int i = 1; i < GenRadial.NumCellsInRadius(radius); i++)
            {
                IntVec3 c = thing.Position + GenRadial.RadialPattern[i];
                if (c.InBounds(thing.Map) && c != ignoreCell && CellBlockedFor(thing, c))
                {
                    count++;
                }
            }
            if (includeCorners)
            {
                IntVec3 bl;
                IntVec3 tl;
                IntVec3 tr;
                IntVec3 br;
                GenAdj.GetAdjacentCorners(thing.Position, out bl, out tl, out tr, out br);
                var corners = new IntVec3[] { bl, tl, tr, br };
                foreach (var corner in corners)
                {
                    if (corner.InBounds(thing.Map) && corner != ignoreCell && CellBlockedFor(thing, corner))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public static List<Thing> GetPawnsInRadius(Map map, IntVec3 position, int radius, IntVec3 ignoreCell, bool includeCorners = false)
        {
            var things = new List<Thing>();
            for (int i = 1; i < GenRadial.NumCellsInRadius(radius); i++)
            {
                IntVec3 c = position + GenRadial.RadialPattern[i];
                if (c.InBounds(map) && c != ignoreCell)
                {
                    Thing blockingThing = map.thingGrid.ThingAt(c, ThingCategory.Pawn);
                    if (blockingThing != null)
                    {
                        things.Add(blockingThing);
                    }
                }
            }
            if (includeCorners)
            {
                IntVec3 bl;
                IntVec3 tl;
                IntVec3 tr;
                IntVec3 br;
                GenAdj.GetAdjacentCorners(position, out bl, out tl, out tr, out br);
                var corners = new IntVec3[] { bl, tl, tr, br };
                foreach (var corner in corners)
                {
                    Thing blockingThing = map.thingGrid.ThingAt(corner, ThingCategory.Pawn);
                    if (blockingThing != null)
                    {
                        things.Add(blockingThing);
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
            for (int i = 1; i < GenRadial.NumCellsInRadius(radius); i++)
            {
                var intVec = position + GenRadial.RadialPattern[i];
                if (intVec != pawn.Position && intVec.InBounds(pawn.Map))
                {
                    if (!CellBlockedFor(pawn, intVec) && !PawnUtility.AnyPawnBlockingPathAt(intVec, pawn, true, false, false))
                    {
                        return intVec;
                    }
                }
            }
            return IntVec3.Invalid;
        }
    }
}
