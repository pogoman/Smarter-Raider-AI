using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PogoAI.Patches
{
    internal class BreachingGrid
    {
        //[HarmonyPatch(typeof(Verse.AI.BreachingGrid), "SetupCostOffsets")]
        //static class BreachingGrid_SetupCostOffsets
        //{
        //    public static bool Prefix(Verse.AI.BreachingGrid __instance)
        //    {
        //        if (__instance.cellCostOffset == null)
        //        {
        //            __instance.cellCostOffset = new IntGrid(__instance.map);
        //        }
        //        __instance.cellCostOffset.Clear(0);
        //        if (!Verse.AI.BreachingGrid.tweakAvoidDangerousRooms)
        //        {
        //            return false;
        //        }
        //        Log.Message($"tweakAvoidDangerousRooms {Verse.AI.BreachingGrid.tweakAvoidDangerousRooms}");
        //        foreach (Room room in __instance.map.regionGrid.allRooms)
        //        {
        //            int num = __instance.DangerousRoomCost(room);
        //            if (num != 0)
        //            {
        //                foreach (IntVec3 c in room.Cells)
        //                {
        //                    __instance.cellCostOffset[c] = num;
        //                }
        //                foreach (IntVec3 c2 in room.BorderCells)
        //                {
        //                    if (c2.InBounds(__instance.map))
        //                    {
        //                        __instance.cellCostOffset[c2] = num;
        //                    }
        //                }
        //            }
        //        }
        //        return false;
        //    }
        //}

        [HarmonyPatch(typeof(Verse.AI.BreachingGrid), "DangerousRoomCost")]
        static class BreachingGrid_DangerousRoomCost
        {
            public static bool Prefix(Room room, ref int __result)
            {
                __result = 0;
                foreach (Thing thing in room.ContainedAndAdjacentThings)
                {
                    Pawn pawn;
                    if ((pawn = thing as Pawn) != null && pawn.mindState != null && !pawn.mindState.Active)
                    {
                        __result += 600;
                    }
                    if (thing.def == ThingDefOf.Hive)
                    {
                        __result += 600;
                    }
                    if (thing.def.ToString().Contains("Turret"))
                    {
                        Log.Message($"thing {thing}");
                        __result += 200;
                    }
                    if (thing.def == ThingDefOf.AncientCryptosleepCasket)
                    {
                        __result += 600;
                    }
                }
                __result = 0;
                return false;
            }
        }
    }
}
