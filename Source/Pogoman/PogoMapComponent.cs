using System.Collections.Generic;
using Verse;

namespace PogoAI
{
    /// <summary>
    /// Per-map mod state. Replaces the old global statics so that multiple maps
    /// (caravan encounters, multi-colony) no longer stomp on each other's caches,
    /// throttles and breach flags. Not saved: state rebuilds after load, same as
    /// the old static behaviour.
    /// </summary>
    public class PogoMapComponent : MapComponent
    {
        public readonly List<Patches.JobGiver_AISapper.CachedPath> pathCostCache = new List<Patches.JobGiver_AISapper.CachedPath>();
        public bool findNewPaths = true;

        public int lastAvoidGridUpdateTicks;

        public bool breachMineables;
        public bool enforceMinimumRange = true;
        public bool doneReset;

        private static Map cachedMap;
        private static PogoMapComponent cachedComp;

        public PogoMapComponent(Map map) : base(map)
        {
            Utilities.RestoreBuildingDestroyerFlags();
        }

        public static PogoMapComponent For(Map map)
        {
            if (map == null)
            {
                return null;
            }
            if (!ReferenceEquals(map, cachedMap))
            {
                cachedComp = map.GetComponent<PogoMapComponent>();
                cachedMap = map;
            }
            return cachedComp;
        }

        public override void MapRemoved()
        {
            base.MapRemoved();
            if (ReferenceEquals(cachedMap, map))
            {
                cachedMap = null;
                cachedComp = null;
            }
        }

        public void ResetBreachState()
        {
            breachMineables = false;
            enforceMinimumRange = true;
            doneReset = false;
        }

        public void ResetSapperState()
        {
            pathCostCache.Clear();
            findNewPaths = true;
        }
    }
}
