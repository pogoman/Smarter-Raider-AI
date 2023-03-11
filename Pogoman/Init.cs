using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PogoAI
{
    public class PogoSettings : ModSettings
    {
        public static string breachWeapons;
        public static bool combatExtendedCompatPerf;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref breachWeapons, "breachWeapons", "bomb, concussion, frag, rocket, inferno, blast, thermal, thump");
            Scribe_Values.Look(ref combatExtendedCompatPerf, "combatExtendedCompatPerf", true);
            base.ExposeData();
        }
    }

    public class Init : Mod
    {
        PogoSettings settings;
        public static bool combatExtended = false;
        public static Harmony harm;

        public Init(ModContentPack contentPack) : base(contentPack)
        {
			Log.Message("POGO INITIALISE");
            harm  = new Harmony("pogo.ai");
            combatExtended = LoadedModManager.RunningMods.FirstOrDefault(m => m.Name == "Combat Extended") != null;
            settings = GetSettings<PogoSettings>();
            harm.PatchAll();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.TextEntryLabeled("Allowed Breach Weapons:\n(comma separated, case insentive, partial matches)", PogoSettings.breachWeapons, 3);
            if (combatExtended)
            {
                listingStandard.CheckboxLabeled("Enable Combat Extended Compatibility Performance fixes\nNote: AI will friendly fire more but performance is much better. Requires game restart.",
                    ref PogoSettings.combatExtendedCompatPerf);
            }
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "All Raids Can Breach";
        }
    }
}
