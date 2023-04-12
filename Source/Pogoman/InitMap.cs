using Verse;
using static PogoAI.Patches.AvoidGrid;

namespace PogoAI
{
    public class InitMap : MapComponent
    {
        public InitMap(Verse.Map map) : base(map)
        {
            Patches.JobGiver_AISapper.pathCostCache.Clear();
            Patches.JobGiver_AISapper.findNewPaths = true;
            AvoidGrid_Regenerate.lastUpdateTicks = 0;
        }
    }
}
