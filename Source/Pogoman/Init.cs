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
        public const string DEFAULT_BREACH_WEAPONS = "bomb, concussion, frag, rocket, inferno, blast, thermal, thump";

        public static string BreachWeapons = DEFAULT_BREACH_WEAPONS;
        public static bool CombatExtendedCompatPerf = true;

        public override void ExposeData()
        {
            base.ExposeData();
            //Scribe_Values.Look(ref BreachWeapons, "breachWeapons", DEFAULT_BREACH_WEAPONS, true);
            Scribe_Values.Look(ref CombatExtendedCompatPerf, "combatExtendedCompatPerf", true, true);
        }
    }

    public class Init : Mod
    {
        PogoSettings settings;
        public static bool combatExtended = false;
        public static Harmony harm;
        string breachWeapons = PogoSettings.BreachWeapons;
        bool combatExtendedCompatPerf = PogoSettings.CombatExtendedCompatPerf;

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
            base.DoSettingsWindowContents(inRect);
            settings = GetSettings<PogoSettings>();
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            //listingStandard.TextEntryLabeled("Allowed Breach Weapons", breachWeapons, 3);
            if (combatExtended)
            {
                listingStandard.CheckboxLabeled("Enable Combat Extended Compatibility Performance fixes\nNote: AI will friendly fire more but performance is much better. Requires game restart.",
                    ref PogoSettings.CombatExtendedCompatPerf);
            }
            listingStandard.End();
            WriteSettings();
        }

        public override string SettingsCategory()
        {
            return "All Raids Can Breach";
        }
    }
}
