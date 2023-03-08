using Mono.Unix.Native;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace PogoAI
{
    public class InEnemyLosPathFinderTuning : PathFinderCostTuning.ICustomizer
    {
        private readonly Map map;
        private readonly BreachingGrid grid;

        public InEnemyLosPathFinderTuning(Map map, Lord lord) 
        {
            this.map = map;
            var lordToilBreach = lord.graph.lordToils.FirstOrDefault(x => x is LordToil_AssaultColonyBreaching) as LordToil_AssaultColonyBreaching;
            if (lordToilBreach != null)
            {
                var data = lordToilBreach.data as LordToilData_AssaultColonyBreaching;
                if (data.breachingGrid.cellCostOffset == null)
                {
                    data.breachingGrid.SetupCostOffsets();
                }
                grid = data.breachingGrid;
            }
        }

        public int CostOffset(IntVec3 from, IntVec3 to)
        {
            if (grid == null)
            {
                return 0;
            }
            var num = 0;
            //Log.Message($"atpoint: {grid.cellCostOffset[to]} 69,0,72: {grid.cellCostOffset[new IntVec3(69,0,73)]} cell count: {grid?.cellCostOffset?.CellsCount}");
            if (to.InBounds(map))
            {
                num += grid.cellCostOffset[to];
            }
            //Log.Message($"Cost offset: {num}");
            return num;
        }
    }
}
