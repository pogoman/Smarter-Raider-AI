using RimWorld;
using System.Collections.Generic;
using Verse;

namespace PogoAI
{
    public static class Utilities
    {
        // verbProps are shared per ThingDef, so flipping ai_IsBuildingDestroyer leaks to every
        // weapon of that def (including player-owned ones) until restart. Record originals so
        // the flags can be restored at raid start / map load; the breach verb finder re-applies
        // them on demand for active raids.
        private static readonly Dictionary<VerbProperties, bool> buildingDestroyerOriginals = new Dictionary<VerbProperties, bool>();

        public static void SetBuildingDestroyer(VerbProperties verbProps, bool value)
        {
            if (!buildingDestroyerOriginals.ContainsKey(verbProps))
            {
                buildingDestroyerOriginals.Add(verbProps, verbProps.ai_IsBuildingDestroyer);
            }
            verbProps.ai_IsBuildingDestroyer = value;
        }

        public static void RestoreBuildingDestroyerFlags()
        {
            foreach (var original in buildingDestroyerOriginals)
            {
                original.Key.ai_IsBuildingDestroyer = original.Value;
            }
        }

        public static bool CellBlockedFor(Thing thing, IntVec3 cell)
        {
            Building edifice = cell.GetEdifice(thing.Map);
            if (edifice != null)
            {
                Building_Door building_Door = edifice as Building_Door;
                var flag = false;
                var flag2 = false;
                if (thing is Pawn pawn)
                {
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

        public static Thing GetNearestThingDesignationDef(Pawn pawn, DesignationCategoryDef category, int radius)
        {
            Building building = null;
            for (int i = 0; i < GenRadial.NumCellsInRadius(radius); i++)
            {
                IntVec3 c = pawn.Position + GenRadial.RadialPattern[i];
                if (c.InBounds(pawn.Map))
                {
                    var edifice = c.GetEdifice(pawn.Map);
                    if (edifice != null && edifice.def.designationCategory == category && edifice.HitPoints > 0)
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
