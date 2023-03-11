//using HarmonyLib;
//using Mono.Unix.Native;
//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;
//using Verse;
//using Verse.AI;
//using Verse.AI.Group;
//using CPF = Verse.AI.CastPositionFinder;

//namespace PogoAI.Patches
//{
//    internal class CastPositionFinderPatch
//    {

//        [HarmonyPatch(typeof(CPF), "EvaluateCell")]
//        static class CastPositionFinder_EvaluateCell
//        { 
//            static bool Prefix(IntVec3 c)
//            {                
//                if (CastPositionFinder.maxRangeFromTargetSquared > 0.01f && CastPositionFinder.maxRangeFromTargetSquared < 250000f && (float)(c - CastPositionFinder.req.target.Position).LengthHorizontalSquared > CastPositionFinder.maxRangeFromTargetSquared)
//                {
//                    if (DebugViewSettings.drawCastPositionSearch)
//                    {
//                        CastPositionFinder.req.caster.Map.debugDrawer.FlashCell(c, 0f, "range target", 50);
//                    }
//                    return false;
//                }
//                if ((double)CastPositionFinder.maxRangeFromLocusSquared > 0.01 && (float)(c - CastPositionFinder.req.locus).LengthHorizontalSquared > CastPositionFinder.maxRangeFromLocusSquared)
//                {
//                    if (DebugViewSettings.drawCastPositionSearch)
//                    {
//                        CastPositionFinder.req.caster.Map.debugDrawer.FlashCell(c, 0.1f, "range home", 50);
//                    }
//                    return false;
//                }
//                if (CastPositionFinder.maxRangeFromCasterSquared > 0.01f)
//                {
//                    CastPositionFinder.rangeFromCasterToCellSquared = (float)(c - CastPositionFinder.req.caster.Position).LengthHorizontalSquared;
//                    if (CastPositionFinder.rangeFromCasterToCellSquared > CastPositionFinder.maxRangeFromCasterSquared)
//                    {
//                        if (DebugViewSettings.drawCastPositionSearch)
//                        {
//                            CastPositionFinder.req.caster.Map.debugDrawer.FlashCell(c, 0.2f, "range caster", 50);
//                        }
//                        return false;
//                    }
//                }
//                if (!c.WalkableBy(CastPositionFinder.req.caster.Map, CastPositionFinder.req.caster))
//                {
//                    return false;
//                }
//                if (CastPositionFinder.req.maxRegions > 0 && c.GetRegion(CastPositionFinder.req.caster.Map, RegionType.Set_Passable).mark != CastPositionFinder.inRadiusMark)
//                {
//                    if (DebugViewSettings.drawCastPositionSearch)
//                    {
//                        CastPositionFinder.req.caster.Map.debugDrawer.FlashCell(c, 0.64f, "reg radius", 50);
//                    }
//                    return false;
//                }
//                if (!CastPositionFinder.req.caster.Map.reachability.CanReach(CastPositionFinder.req.caster.Position, c, PathEndMode.OnCell, TraverseParms.For(CastPositionFinder.req.caster, Danger.Some, TraverseMode.ByPawn, false, false, false)))
//                {
//                    if (DebugViewSettings.drawCastPositionSearch)
//                    {
//                        CastPositionFinder.req.caster.Map.debugDrawer.FlashCell(c, 0.4f, "can't reach", 50);
//                    }
//                    return false;
//                }
//                float num = CastPositionFinder.CastPositionPreference(c);
//                //if (CastPositionFinder.avoidGrid != null)
//                //{
//                //    byte b = CastPositionFinder.avoidGrid[c];
//                //    num *= Mathf.Max(0.1f, (37.5f - (float)b) / 37.5f);
//                //}
//                if (DebugViewSettings.drawCastPositionSearch)
//                {
//                    CastPositionFinder.req.caster.Map.debugDrawer.FlashCell(c, num / 4f, num.ToString("F3"), 50);
//                }
//                if (num < CastPositionFinder.bestSpotPref)
//                {
//                    return false;
//                }
//                if (!CastPositionFinder.verb.CanHitTargetFrom(c, CastPositionFinder.req.target))
//                {
//                    if (DebugViewSettings.drawCastPositionSearch)
//                    {
//                        CastPositionFinder.req.caster.Map.debugDrawer.FlashCell(c, 0.6f, "can't hit", 50);
//                    }
//                    return false;
//                }
//                if (!CastPositionFinder.req.caster.Map.pawnDestinationReservationManager.CanReserve(c, CastPositionFinder.req.caster, false))
//                {
//                    if (DebugViewSettings.drawCastPositionSearch)
//                    {
//                        CastPositionFinder.req.caster.Map.debugDrawer.FlashCell(c, num * 0.9f, "resvd", 50);
//                    }
//                    return false;
//                }
//                if (PawnUtility.KnownDangerAt(c, CastPositionFinder.req.caster.Map, CastPositionFinder.req.caster))
//                {
//                    if (DebugViewSettings.drawCastPositionSearch)
//                    {
//                        CastPositionFinder.req.caster.Map.debugDrawer.FlashCell(c, 0.9f, "danger", 50);
//                    }
//                    return false;
//                }
//                if (CastPositionFinder.req.validator != null && !CastPositionFinder.req.validator(c))
//                {
//                    return false;
//                }
//                CastPositionFinder.bestSpot = c;
//                CastPositionFinder.bestSpotPref = num;
//                return false;
//            }

//        }

//                //[HarmonyPatch(typeof(CPF), "TryFindCastPosition")]
//                //static class CastPositionFinder_TryFindCastPosition
//                //{
//                //    static ShootLineCastPositionFinder shootLineCastPositionFinder = new ShootLineCastPositionFinder();

//                //    static void Prefix(CastPositionRequest newReq, out IntVec3 dest)
//                //    {
//                //        dest = CPF.bestSpot;
//                //        if (CPF.bestSpot.IsValid)
//                //        {
//                //            dest = shootLineCastPositionFinder.GetShootLine(newReq);
//                //        }
//                //    }

//                //    public class ShootLineCastPositionFinder
//                //    {
//                //        HashSet<IntVec3> IgnoreCells = new HashSet<IntVec3>();
//                //        Func<IntVec3, bool> IgnoreCellsFunc;

//                //        public ShootLineCastPositionFinder()
//                //        {
//                //            IgnoreCellsFunc = new Func<IntVec3, bool>(this.InIgnoreCells);
//                //        }

//                //        private bool InIgnoreCells(IntVec3 c)
//                //        {
//                //            return !IgnoreCells.Contains(c);
//                //        }

//                //        public IntVec3 GetShootLine(CastPositionRequest req)
//                //        {
//                //            req.validator = IgnoreCellsFunc;
//                //            IntVec3 intVec = CPF.bestSpot;
//                //            ShootLine shootLine;
//                //            var check = req.verb.TryFindShootLineFromTo(CPF.bestSpot, CPF.targetLoc, out shootLine);
//                //            if (!check)
//                //            {
//                //                IgnoreCells.Add(CPF.bestSpot);
//                //                Log.Message($"bad shootline: {CPF.bestSpot}, {CPF.targetLoc} {CPF.casterLoc} igcount: {IgnoreCells.Count}");
//                //                CPF.TryFindCastPosition(req, out intVec);
//                //            }
//                //            else
//                //            {
//                //                Log.Message($"Good shootline: {CPF.bestSpot}, {CPF.targetLoc} {CPF.casterLoc} igcount: {IgnoreCells.Count}");
//                //            }
//                //            return intVec;
//                //        }
//                //    }
//                //} 

//            }
//}
