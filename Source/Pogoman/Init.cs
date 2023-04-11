using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using PogoAI.Extensions;
using RimWorld;
using UnityEngine;
using UnityEngine.SceneManagement;
using Verse;

namespace PogoAI
{
    public class PogoSettings : ModSettings
    {
        public const string DEFAULT_BREACH_WEAPONS = "stickbomb, concussion, doom, triple, inferno, chargeblast, thermal, thump, cannon";

        public string BreachWeapons = DEFAULT_BREACH_WEAPONS;
        public bool CombatExtendedCompatPerf = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref BreachWeapons, "breachWeapons", DEFAULT_BREACH_WEAPONS, true);
            Scribe_Values.Look(ref CombatExtendedCompatPerf, "combatExtendedCompatPerf", true, true);
        }
    }

    public class Init : Mod
    {
        public static PogoSettings settings;
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
            base.DoSettingsWindowContents(inRect);
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);    
            listingStandard.AddLabeledTextField("Allowed Breach Weapons:\n(comma separated, case insensitive, partial match, no spaces)", ref settings.BreachWeapons, 0.25f, 80);
            if (combatExtended)
            {
                listingStandard.CheckboxLabeled("Enable Combat Extended Compatibility Performance fix (recommeded to leave on)\nRequires game restart.",
                    ref settings.CombatExtendedCompatPerf);
            }
            listingStandard.End();
            settings.Write();
        }

        public override string SettingsCategory()
        {
            return "Smarter Raider AI";
        }
    }
}
