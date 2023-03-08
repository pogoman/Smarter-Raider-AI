using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PogoAI
{
    public class Init : Mod
    {
       
        public Init(ModContentPack contentPack) : base(contentPack)
        {
			Log.Message("POGO INITIALISE");
            var harm  = new Harmony("pogo.ai");
			harm.PatchAll();
        }

    }
}
