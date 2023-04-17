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

        public int maxSappers = 20;
        public string breachWeapons = DEFAULT_BREACH_WEAPONS;
        public bool combatExtendedCompatPerf = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref breachWeapons, "breachWeapons", DEFAULT_BREACH_WEAPONS, true);
            Scribe_Values.Look(ref combatExtendedCompatPerf, "combatExtendedCompatPerf", true, true);
            Scribe_Values.Look(ref maxSappers, "maxSappers", 20, true);
        }
    }

    public class Init : Mod
    {
        public static PogoSettings settings;
        public static bool combatExtended = false;
        public static bool CAI = false;
        public static Harmony harm;

        public Init(ModContentPack contentPack) : base(contentPack)
        {
            Log.Message("Smarter Raider AI Initialising...");
            harm  = new Harmony("pogo.ai");
            combatExtended = LoadedModManager.RunningMods.FirstOrDefault(m => m.PackageId.Matches("CETeam.CombatExtended")) != null;
            if (combatExtended)
            {
                Log.Message("SRAI: CE detected");
            }
            CAI = LoadedModManager.RunningMods.FirstOrDefault(m => m.PackageId.Matches("Krkr.rule56")) != null;
            if (CAI)
            {
                Log.Message("SRAI: CAI detected");
            }
            settings = GetSettings<PogoSettings>();
            harm.PatchAll();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);    
            listingStandard.AddLabeledTextField("Allowed Breach Weapons:\n(comma separated, case insensitive, partial match, no spaces)", ref settings.breachWeapons, 0.25f, 80);
            listingStandard.SliderLabeled("Maximum number of sappers per raid:\n(Higher numbers may affect performance)\n", ref settings.maxSappers, "{0}/50", 1, 50);
            if (combatExtended)
            {
                listingStandard.CheckboxLabeled("Enable Combat Extended Compatibility Performance fix: \n(recommeded to leave on. Requires game restart.)",
                    ref settings.combatExtendedCompatPerf);
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
