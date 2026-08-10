using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using PogoAI.Extensions;
using RimWorld;
using UnityEngine;
using Verse;

namespace PogoAI
{
    public class PogoSettings : ModSettings
    {
        public const string DEFAULT_BREACH_WEAPONS = "stickbomb, concussion, doom, triple, inferno, chargeblast, thermal, thump, cannon";
        public const int AVOID_DEFAULT_COST = 45;

        public bool everyRaidSaps = true;
        public int maxSappers = 20;
        public string maxSappersBuf;
        public float reactionMinSeconds = 1.2f;
        public float reactionMaxSeconds = 2.4f;
        public string reactionMinBuf;
        public string reactionMaxBuf;
        public string breachWeapons = DEFAULT_BREACH_WEAPONS;
        public TechLevel minSmartTechLevel = TechLevel.Neolithic;
        public int costLOS = AVOID_DEFAULT_COST;
        public string costLOSBuf;

        public int reactionMin => (int)(reactionMinSeconds * 100);

        public int reactionMax => (int)(reactionMaxSeconds * 100);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref everyRaidSaps, "everyRaidSaps", true, true);
            Scribe_Values.Look(ref breachWeapons, "breachWeapons", DEFAULT_BREACH_WEAPONS, true);
            Scribe_Values.Look(ref maxSappers, "maxSappers", 20, true);
            Scribe_Values.Look(ref reactionMinSeconds, "reactionMinSeconds", 1.2f, true);
            Scribe_Values.Look(ref reactionMaxSeconds, "reactionMaxSeconds", 2.4f, true);
            Scribe_Values.Look<TechLevel>(ref minSmartTechLevel, "minSmartTechLevel", TechLevel.Neolithic, true);
            Scribe_Values.Look(ref costLOS, "costLOS", AVOID_DEFAULT_COST, true);
        }
    }

    public class Init : Mod
    {
        public static PogoSettings settings;
        public static bool combatExtended = false;
        public static bool combatAi = false;
        public static Harmony harmony;

        private static readonly TechLevel[] smartTechLevels =
        {
            TechLevel.Neolithic, TechLevel.Medieval, TechLevel.Industrial,
            TechLevel.Spacer, TechLevel.Ultra, TechLevel.Archotech
        };

        public Init(ModContentPack contentPack) : base(contentPack)
        {
            Log.Message("Smarter Raider AI Initialising...");
            harmony = new Harmony("pogo.ai");
            combatExtended = LoadedModManager.RunningMods.FirstOrDefault(m => m.PackageId.Matches("CETeam.CombatExtended")) != null;
            if (combatExtended)
            {
                Log.Message("SRAI: CE detected");
            }
            combatAi = LoadedModManager.RunningMods.FirstOrDefault(m => m.PackageId.Matches("Krkr.rule56")) != null;
            if (combatAi)
            {
                Log.Message("SRAI: CAI detected");
            }
            settings = GetSettings<PogoSettings>();
            harmony.PatchAll();
            // Startup self-check: if a patch target ever disappears in a game update,
            // PatchAll throws above; this count makes a partial/silent failure visible too.
            Log.Message($"SRAI: {harmony.GetPatchedMethods().Count()} methods patched");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("Every raid can sap/dig:", ref settings.everyRaidSaps);
            listingStandard.TextFieldNumericLabeled("Maximum number of sappers per raid (def 20): ", ref settings.maxSappers, ref settings.maxSappersBuf, 0, 100);
            listingStandard.AddLabeledTextField("Allowed Breach Weapons:\n(comma separated, case insensitive, partial match, no spaces)", ref settings.breachWeapons, 0.25f, 80);
            if (listingStandard.ButtonTextLabeled("Minimum Smart Raid Tech Level:\n(tech levels that use the avoid grid)", settings.minSmartTechLevel.ToString(), TextAnchor.UpperLeft, (string)null, (string)null))
            {
                var options = new List<FloatMenuOption>();
                foreach (var techLevel in smartTechLevels)
                {
                    var level = techLevel;
                    options.Add(new FloatMenuOption(level.ToString(), () => settings.minSmartTechLevel = level));
                }
                Find.WindowStack.Add(new FloatMenu(options)
                {
                    vanishIfMouseDistant = true
                });
            }
            listingStandard.TextFieldNumericLabeled("Minimum reaction time (def 1.2): ", ref settings.reactionMinSeconds, ref settings.reactionMinBuf, 0.1f);
            listingStandard.TextFieldNumericLabeled("Maximum reaction time (def 2.4): ", ref settings.reactionMaxSeconds, ref settings.reactionMaxBuf, 0.1f);
            listingStandard.TextFieldNumericLabeled<int>($"Pawn/Turret LOS pathfinding cell cost values (def {PogoSettings.AVOID_DEFAULT_COST}): ", ref settings.costLOS, ref settings.costLOSBuf);
            listingStandard.Label("Note: Any updates require a game restart. Reaction time settings may affect performance.\n");
            listingStandard.End();
        }

        public override string SettingsCategory()
        {
            return "Smarter Raider AI";
        }
    }
}
