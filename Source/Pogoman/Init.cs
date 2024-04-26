using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            Scribe_Values.Look(ref costLOS, "costLOS", AVOID_DEFAULT_COST, true);
        }
    }

    public class Init : Mod
    {
        public static PogoSettings settings;
        public static bool combatExtended = false;
        public static bool combatAi = false;
        public static Harmony harmony;

        public Init(ModContentPack contentPack) : base(contentPack)
        {
            Log.Message("Smarter Raider AI Initialising...");
            harmony  = new Harmony("pogo.ai");
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
            patchPrivateClass(typeof(BreachingUtility), typeof(Patches.BreachingUtility.BreachRangedCastPositionFinder_TryFindRangedCastPosition), "RimWorld.BreachingUtility+BreachRangedCastPositionFinder", "TryFindRangedCastPosition", "Postfix");
            patchPrivateClass(typeof(BreachingUtility), typeof(Patches.BreachingUtility.BreachRangedCastPositionFinder_SafeForRangedCast), "RimWorld.BreachingUtility+BreachRangedCastPositionFinder", "SafeForRangedCast", "Prefix");
            patchPrivateMethod(typeof(RimWorld.JobGiver_AIFightEnemy), typeof(Patches.JobGiver_AIFightEnemy.JobGiver_AIFightEnemy_TryGiveJob), "TryGiveJob", "Prefix");
            patchPrivateMethod(typeof(RimWorld.JobGiver_AIFightEnemy), typeof(Patches.JobGiver_AIFightEnemy.JobGiver_AIFightEnemy_TryGiveJob), "TryGiveJob", "Postfix");
        }

        private void patchPrivateMethod(Type classType, Type myClass, string methodName, string harmonyMethod)
        {
            // Access the private method
            var methodToPatch = classType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodToPatch == null)
            {
                // Handle the error if the method is not found
                throw new InvalidOperationException($"Method '{methodName}' not found.");
            }

            // Apply the patch
            if (harmonyMethod == "Prefix")
            {
                var prefix = new HarmonyMethod(myClass.GetMethod(harmonyMethod, BindingFlags.Static | BindingFlags.NonPublic));
                harmony.Patch(methodToPatch, prefix, null);
            }
            else
            {
                var postfix = new HarmonyMethod(myClass.GetMethod(harmonyMethod, BindingFlags.Static | BindingFlags.NonPublic));
                harmony.Patch(methodToPatch, null, postfix);
            }
        }

        private void patchPrivateClass(Type parentClass, Type myClass, string privateClassName, string methodName, string harmonyMethod)
        {
            // Access the private class
            var targetType = parentClass.Assembly.GetType(privateClassName);
            if (targetType == null)
            {
                // Handle the error if the class is not found
                throw new InvalidOperationException($"Private class '{privateClassName}' not found.");
            }

            // Access the private method
            var methodToPatch = targetType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodToPatch == null)
            {
                // Handle the error if the method is not found
                throw new InvalidOperationException($"Method '{methodName}' not found.");
            }

            // Apply the patch
            if (harmonyMethod == "Prefix")
            {
                var prefix = new HarmonyMethod(myClass.GetMethod(harmonyMethod, BindingFlags.Static | BindingFlags.NonPublic));
                harmony.Patch(methodToPatch, prefix, null);
            }
            else
            {
                var postfix = new HarmonyMethod(myClass.GetMethod(harmonyMethod, BindingFlags.Static | BindingFlags.NonPublic));
                harmony.Patch(methodToPatch, null, postfix);
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);    
            listingStandard.CheckboxLabeled("Every raid can sap/dig:", ref settings.everyRaidSaps);
            listingStandard.SliderLabeled("Maximum number of sappers per raid:\n(Higher numbers may affect performance)", settings.maxSappers, 1, 50);
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
            listingStandard.End();
            settings.Write();
        }

        public override string SettingsCategory()
        {
            return "Smarter Raider AI";
        }
    }
}
