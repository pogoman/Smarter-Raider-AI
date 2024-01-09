using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using PogoAI.Extensions;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PogoAI
{
    public class PogoSettings : ModSettings
    {
        public const string DEFAULT_BREACH_WEAPONS = "stickbomb, concussion, doom, triple, inferno, chargeblast, thermal, thump, cannon";
        public const int AVOID_DEFAULT_COST = 45;

        public bool everyRaidSaps = true;
        public int maxSappers = 20;
        public string breachWeapons = DEFAULT_BREACH_WEAPONS;
        public bool combatExtendedCompatPerf = true;
        public TechLevel minSmartTechLevel = TechLevel.Neolithic;
        public int costOffLordWalkGrid = PathFinderCostTuning.Cost_OffLordWalkGrid;
        public string costOffLordWalkGridBuf;
        public float costBlockedDoorPerHitPoint = PathFinderCostTuning.Cost_BlockedDoorPerHitPoint;
        public string costBlockedDoorPerHitPointBuf;
        public int costBlockedWallExtraForNaturalWalls = PathFinderCostTuning.Cost_BlockedWallExtraForNaturalWalls;
        public string costBlockedWallExtraForNaturalWallsBuf;
        public float costBlockedWallExtraPerHitPoint = PathFinderCostTuning.Cost_BlockedWallExtraPerHitPoint;
        public string costBlockedWallExtraPerHitPointBuf;
        public int costBlockedWallBase = PathFinderCostTuning.Cost_BlockedWallBase;
        public string costBlockedWallBaseBuf;
        public int costBlockedDoor = PathFinderCostTuning.Cost_BlockedDoor;
        public string costBlockedDoorBuf;
        public int costLOS = AVOID_DEFAULT_COST;
        public string costLOSBuf;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref everyRaidSaps, "everyRaidSaps", true, true);
            Scribe_Values.Look(ref breachWeapons, "breachWeapons", DEFAULT_BREACH_WEAPONS, true);
            Scribe_Values.Look(ref combatExtendedCompatPerf, "combatExtendedCompatPerf", true, true);
            Scribe_Values.Look(ref maxSappers, "maxSappers", 20, true);
            Scribe_Values.Look<TechLevel>(ref minSmartTechLevel, "minSmartTechLevel", TechLevel.Neolithic, true);
            Scribe_Values.Look(ref costOffLordWalkGrid, "costOffLordWalkGrid", PathFinderCostTuning.Cost_OffLordWalkGrid, true);
            Scribe_Values.Look(ref costBlockedDoorPerHitPoint, "costBlockedDoorPerHitPoint", PathFinderCostTuning.Cost_BlockedDoorPerHitPoint, true);
            Scribe_Values.Look(ref costBlockedWallExtraForNaturalWalls, "costBlockedWallExtraForNaturalWalls", PathFinderCostTuning.Cost_BlockedWallExtraForNaturalWalls, true);
            Scribe_Values.Look(ref costBlockedWallExtraPerHitPoint, "costBlockedWallExtraPerHitPoint", PathFinderCostTuning.Cost_BlockedWallExtraPerHitPoint, true);
            Scribe_Values.Look(ref costBlockedWallBase, "costBlockedWallBase", PathFinderCostTuning.Cost_BlockedWallBase, true);
            Scribe_Values.Look(ref costBlockedDoor, "costBlockedDoor", PathFinderCostTuning.Cost_BlockedDoor, true);
            Scribe_Values.Look(ref costLOS, "costLOS", AVOID_DEFAULT_COST, true);
        }
    }

    public class Init : Mod
    {
        public static PogoSettings settings;
        public static bool combatExtended = false;
        public static bool combatAi = false;
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
            combatAi = LoadedModManager.RunningMods.FirstOrDefault(m => m.PackageId.Matches("Krkr.rule56")) != null;
            if (combatAi)
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
            listingStandard.CheckboxLabeled("Every raid can sap/dig:", ref settings.everyRaidSaps);
            listingStandard.SliderLabeled("Maximum number of sappers per raid:\n(Higher numbers may affect performance)", ref settings.maxSappers, "{0}/50", 1, 50);
            listingStandard.AddLabeledTextField("Allowed Breach Weapons:\n(comma separated, case insensitive, partial match, no spaces)", ref settings.breachWeapons, 0.25f, 80);
            if (listingStandard.ButtonTextLabeled("Minimum Smart Raid Tech Level:\n(tech levels that use the avoid grid)", settings.minSmartTechLevel.ToString(), TextAnchor.UpperLeft, (string)null, (string)null))
            {
                List<FloatMenuOption> floatMenuOptionList = new List<FloatMenuOption>();
                floatMenuOptionList.Add(new FloatMenuOption("Neolithic", (Action)(() => settings.minSmartTechLevel = TechLevel.Neolithic), (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0.0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0));
                floatMenuOptionList.Add(new FloatMenuOption("Medieval", (Action)(() => settings.minSmartTechLevel = TechLevel.Medieval), (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0.0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0));
                floatMenuOptionList.Add(new FloatMenuOption("Industrial", (Action)(() => settings.minSmartTechLevel = TechLevel.Industrial), (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0.0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0));
                floatMenuOptionList.Add(new FloatMenuOption("Spacer", (Action)(() => settings.minSmartTechLevel = TechLevel.Spacer), (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0.0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0));
                floatMenuOptionList.Add(new FloatMenuOption("Ultra", (Action)(() => settings.minSmartTechLevel = TechLevel.Ultra), (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0.0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0));
                floatMenuOptionList.Add(new FloatMenuOption("Archotech", (Action)(() => settings.minSmartTechLevel = TechLevel.Archotech), (MenuOptionPriority)4, (Action<Rect>)null, (Thing)null, 0.0f, (Func<Rect, bool>)null, (WorldObject)null, true, 0));
                Find.WindowStack.Add((Window)new FloatMenu(floatMenuOptionList)
                {
                    vanishIfMouseDistant = true
                });
            }
            if (combatExtended)
            {
                listingStandard.CheckboxLabeled("Enable Combat Extended Compatibility Performance fix: \n(recommeded to leave on. Requires game restart.)",
                    ref settings.combatExtendedCompatPerf);
            }
            listingStandard.Label("WARNING: Advanced settings below, change at your own risk. Any updates require a game restart.\n\nPathfinding algorithm cell cost values:\n");
            listingStandard.TextFieldNumericLabeled<int>($"Pawn/Turret LOS (additive on intersect) (def: {PogoSettings.AVOID_DEFAULT_COST})", ref settings.costLOS, ref settings.costLOSBuf);
            listingStandard.TextFieldNumericLabeled<int>($"OffLordWalkGrid (def: {PathFinderCostTuning.Cost_OffLordWalkGrid})", ref settings.costOffLordWalkGrid, ref settings.costOffLordWalkGridBuf);
            listingStandard.TextFieldNumericLabeled<float>($"BlockedDoorPerHitPoint (def: {PathFinderCostTuning.Cost_BlockedDoorPerHitPoint})", ref settings.costBlockedDoorPerHitPoint, ref settings.costBlockedDoorPerHitPointBuf);
            listingStandard.TextFieldNumericLabeled<int>($"BlockedWallExtraForNaturalWalls (def: {PathFinderCostTuning.Cost_BlockedWallExtraForNaturalWalls})", ref settings.costBlockedWallExtraForNaturalWalls, ref settings.costBlockedWallExtraForNaturalWallsBuf);
            listingStandard.TextFieldNumericLabeled<float>($"BlockedWallExtraPerHitPoint (def: {PathFinderCostTuning.Cost_BlockedWallExtraPerHitPoint})", ref settings.costBlockedWallExtraPerHitPoint, ref settings.costBlockedWallExtraPerHitPointBuf);
            listingStandard.TextFieldNumericLabeled<int>($"BlockedWallBase (def: {PathFinderCostTuning.Cost_BlockedWallBase})", ref settings.costBlockedWallBase, ref settings.costBlockedWallBaseBuf);
            listingStandard.TextFieldNumericLabeled<int>($"BlockedDoor (def: {PathFinderCostTuning.Cost_BlockedDoor})", ref settings.costBlockedDoor, ref settings.costBlockedDoorBuf);
            listingStandard.End();
            settings.Write();
        }

        public override string SettingsCategory()
        {
            return "Smarter Raider AI";
        }
    }
}
