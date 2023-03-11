using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PogoAI
{
    public class Init : Mod
    {

        public static bool CombatExtended;
        public static Harmony harm;

        public Init(ModContentPack contentPack) : base(contentPack)
        {
			Log.Message("POGO INITIALISE");
            harm  = new Harmony("pogo.ai");
            Init.CombatExtended = LoadedModManager.RunningMods.FirstOrDefault(m => m.Name == "Combat Extended") != null;
            harm.PatchAll();
        }

    }
}
