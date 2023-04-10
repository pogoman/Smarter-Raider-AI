using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace PogoAI
{
    public class InitMap : MapComponent
    {
        public InitMap(Verse.Map map) : base(map)
        {
            Patches.JobGiver_AISapper.pathCostCache.Clear();
        }
    }
}
