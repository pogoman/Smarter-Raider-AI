using PogoAI.Patches;
using Verse;

namespace PogoAI
{
    public class InitMap : MapComponent
    {
        public InitMap(Verse.Map map) : base(map)
        {
            JobGiver_AISapper.pathCostCache.Clear();
            JobGiver_AISapper.findNewPaths = true;
            AvoidGrid_Regenerate.lastUpdateTicks = 0;
        }
    }
}
