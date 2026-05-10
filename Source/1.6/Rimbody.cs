using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace Maux36.Rimbody
{
    public class Rimbody : Mod
    {
        public static RimbodySettings settings;
        public static bool BodyChangeActive;
        public static bool StaminaActive;
        public static bool IndividualityLoaded = false;
        public static bool WayBetterRomanceLoaded = false;
        public static bool ExosuitFrameworkLoaded = false;
        public static bool CombatExtendedLoaded = false;

        public Rimbody(ModContentPack content) : base(content)
        {

            settings = GetSettings<RimbodySettings>();

            if (ModsConfig.IsActive("erdelf.humanoidalienraces"))
            {
                HARCompat.Activate();
                HARCompat.Active = true;
            }

            if (ModsConfig.IsActive("mlie.syrindividuality")) IndividualityLoaded = true;
            if (ModsConfig.IsActive("divinederivative.romance")) WayBetterRomanceLoaded = true;
            if (ModsConfig.IsActive("aoba.exosuit.framework")) ExosuitFrameworkLoaded = true;
            if (ModsConfig.IsActive("ceteam.combatextended")) CombatExtendedLoaded = true;
        }

        public override string SettingsCategory()
        {
            return "RimbodySettingCategory".Translate();
        }

        private static Vector2 leftScrollPosition = Vector2.zero;
        private static Vector2 rightScrollPosition = Vector2.zero;
        private static float totalContentHeight = ModsConfig.BiotechActive?820f:770f;
        private const float ScrollBarWidthMargin = 18f;
        private const float divider = 0.55f;
        private const float labelPCT = 0.7f;
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing_Standard = new Listing_Standard();
            Rect leftOutRect = new Rect(10f, 50f, inRect.width * divider, inRect.height - 60f);
            Rect leftViewRect = new Rect(0f, 0f, leftOutRect.width - 20f, 700f);
            bool leftscrollBarVisible = totalContentHeight > leftOutRect.height;
            var leftscrollViewTotal = new Rect(0f, 0f, leftOutRect.width - (leftscrollBarVisible ? ScrollBarWidthMargin : 0), totalContentHeight);
            Widgets.BeginScrollView(leftOutRect, ref leftScrollPosition, leftscrollViewTotal);
            listing_Standard.Begin(leftscrollViewTotal);
            listing_Standard.Gap(12f);

            listing_Standard.Label("RimbodyGeneralSetting".Translate());
            listing_Standard.Gap(12f);
            RimbodySettings.rateFactor = (float)Math.Round(listing_Standard.SliderLabeled("RimbodyRateFactor".Translate() + " ("+"Default".Translate()+" 1): " + RimbodySettings.rateFactor, RimbodySettings.rateFactor, 0.1f, 5f, labelPCT, tooltip: "RimbodyRateFactorTooltip".Translate()), 1);
            listing_Standard.Gap(6f);
            RimbodySettings.WorkOutGainEfficiency = (float)Math.Round(listing_Standard.SliderLabeled("RimbodyWorkOutGainEfficiency".Translate() + " (" + "Default".Translate() + " 1.0): " + RimbodySettings.WorkOutGainEfficiency, RimbodySettings.WorkOutGainEfficiency, 1f, 2f, labelPCT, tooltip: "RimbodyWorkOutGainEfficiencyTooltip".Translate()), 1);
            listing_Standard.Gap(6f);
            RimbodySettings.carryRateMultiplier = Mathf.Clamp((float)Math.Round(listing_Standard.SliderLabeled("RimbodyCarryRateMultiplier".Translate() + " (" + "Default".Translate() + " 1.0): " + RimbodySettings.carryRateMultiplier, RimbodySettings.carryRateMultiplier, 0f, 1f, labelPCT, tooltip: "RimbodyCarryRateMultiplierTooltip".Translate()), 1), 0f, 1f);
            listing_Standard.Gap(6f);
            listing_Standard.CheckboxLabeled("RimbodyGenderDifference".Translate(), ref RimbodySettings.genderDifference, "RimbodyGenderDifferenceTooltip".Translate());
            listing_Standard.Gap(6f);
            listing_Standard.CheckboxLabeled("RimbodyShowFleck".Translate(), ref RimbodySettings.showFleck, "RimbodyShowFleckTooltip".Translate());
            listing_Standard.Gap(6f);
            listing_Standard.CheckboxLabeled("RimbodyRecreationExercise".Translate(), ref RimbodySettings.workoutDuringRecTime, "RimbodyRecreationExerciseToolTip".Translate());
            listing_Standard.Gap(6f);
            listing_Standard.CheckboxLabeled("RimbodyTARecreationSelect".Translate(), ref RimbodySettings.useRecToSelect, "RimbodyTARecreationSelectToolTip".Translate());
            listing_Standard.Gap(6f);
            //listing_Standard.CheckboxLabeled("RimbodyUseExhaustion".Translate(), ref RimbodySettings.useExhaustion, "RimbodyUseExhaustionTooltip".Translate());
            //listing_Standard.Gap(6f);
            listing_Standard.CheckboxLabeled("RimbodyUseFatigue".Translate(), ref RimbodySettings.useFatigue, "RimbodyUseFatigueTooltip".Translate());
            listing_Standard.Gap(24f);

            listing_Standard.Label("RimbodyPerformanceSetting".Translate());
            listing_Standard.Gap(12f);
            if (listing_Standard.RadioButton("RimbodyModePerformance".Translate(), RimbodySettings.CalcEveryTick == 150, 0f, "RimbodyModeTooltipPerformance".Translate()))
            {
                RimbodySettings.CalcEveryTick = 150;
            }
            listing_Standard.Gap(6f);
            if (listing_Standard.RadioButton("RimbodyModeOptimized".Translate(), RimbodySettings.CalcEveryTick == 75, 0f, "RimbodyModeTooltipOptimized".Translate()))
            {
                RimbodySettings.CalcEveryTick = 75;
            }
            listing_Standard.Gap(6f);
            if (listing_Standard.RadioButton("RimbodyModePrecision".Translate(), RimbodySettings.CalcEveryTick == 30, 0f, "RimbodyModeTooltipPrecision".Translate()))
            {
                RimbodySettings.CalcEveryTick = 30;
            }
            listing_Standard.Gap(6f);
            if (listing_Standard.RadioButton("RimbodyModeUltra".Translate(), RimbodySettings.CalcEveryTick == 15, 0f, "RimbodyModeTooltipUltra".Translate()))
            {
                RimbodySettings.CalcEveryTick = 15;
            }
            listing_Standard.Gap(24f);

            listing_Standard.Label("RimbodyThreshholdSetting".Translate());
            listing_Standard.Gap(12f);
            RimbodySettings.fatThresholdFat = (float)Math.Round(listing_Standard.SliderLabeled("RimbodyFatThreshholdFat".Translate()+ " (" + "Default".Translate() + " 35): " + RimbodySettings.fatThresholdFat, RimbodySettings.fatThresholdFat, 25, 50, labelPCT, tooltip: "RimbodyFatThreshholdFatTooltip".Translate()),1);
            RimbodySettings.fatThresholdThin = (float)Math.Round(listing_Standard.SliderLabeled("RimbodyFatThresholdThin".Translate() + " (" + "Default".Translate() + " 15): " + RimbodySettings.fatThresholdThin, RimbodySettings.fatThresholdThin, 0, 25, labelPCT, tooltip: "RimbodyFatThresholdThinTooltip".Translate()), 1);
            listing_Standard.Gap(6f);
            RimbodySettings.muscleThresholdHulk = (float)Math.Round(listing_Standard.SliderLabeled("RimbodyMuscleThresholdHulk".Translate() + " (" + "Default".Translate() + " 35): " + RimbodySettings.muscleThresholdHulk, RimbodySettings.muscleThresholdHulk, 25, 50, labelPCT, tooltip: "RimbodyMuscleThresholdHulkTooltip".Translate()), 1);
            RimbodySettings.muscleThresholdThin = (float)Math.Round(listing_Standard.SliderLabeled("RimbodyMuscleThresholdThin".Translate() + " (" + "Default".Translate() + " 15): " + RimbodySettings.muscleThresholdThin, RimbodySettings.muscleThresholdThin, 0, 25, labelPCT, tooltip: "RimbodyMuscleThresholdThinTooltip".Translate()), 1);
            listing_Standard.Gap(6f);
            RimbodySettings.gracePeriod = (float)Math.Round(listing_Standard.SliderLabeled("RimbodyGracePeriod".Translate() + " (" + "Default".Translate() + " 1): " + RimbodySettings.gracePeriod, RimbodySettings.gracePeriod, 0.1f, 5, labelPCT, tooltip: "RimbodyGracePeriodTooltip".Translate()), 1);

            if (ModsConfig.BiotechActive)
            {
                listing_Standard.Gap(24f);
                RimbodySettings.nonSenescentpoint = (int)Math.Round(listing_Standard.SliderLabeled("RimbodyNonSenescentpoint".Translate() + " (" + "Default".Translate() + " 25): " + RimbodySettings.nonSenescentpoint, RimbodySettings.nonSenescentpoint, 13, 60, labelPCT, tooltip: "RimbodyNonSenescentpointTooltip".Translate()), 0);
            }

            listing_Standard.Gap(24f);
            if (listing_Standard.ButtonText("RimbodyDefaultSetting".Translate(), "RimbodyDefaultSettingTooltip".Translate()))
            {
                RimbodySettings.rateFactor = 1f;
                RimbodySettings.WorkOutGainEfficiency = 1.0f;
                RimbodySettings.carryRateMultiplier = 1.0f;
                RimbodySettings.genderDifference = true;
                RimbodySettings.showFleck = true;
                RimbodySettings.useFatigue = true;
                RimbodySettings.CalcEveryTick = 75;
                RimbodySettings.fatThresholdFat = 35f;
                RimbodySettings.fatThresholdThin = 15f;
                RimbodySettings.muscleThresholdHulk = 35f;
                RimbodySettings.muscleThresholdThin = 15f;
                RimbodySettings.gracePeriod = 1f;
                RimbodySettings.nonSenescentpoint = 25;
            }

            listing_Standard.End();
            Widgets.EndScrollView();
            Rect warningRect = new Rect(leftOutRect.xMax+10f, 0f, inRect.width - leftOutRect.width - 20f, 50f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Medium;
            GUI.color = Color.red;
            Widgets.Label(warningRect, "Requires Restart");
            GUI.color = Color.white;
            Rect rect2 = new Rect(warningRect.x, 50f, warningRect.width, 50f);
            listing_Standard.Begin(rect2);
            if (listing_Standard.ButtonTextLabeled("Enable/Disable Rimbody Features:", "Reset"))
            {
                RimbodySettings.raceOption.Clear();
                CompToHumanlikes.GenerateRaceSettings(false);
            }
            listing_Standard.End();
            Rect outRect = new Rect(warningRect.x, 100f, warningRect.width, inRect.height - 10f);
            Rect val = new Rect(0f, 0f, outRect.width - 30f, (float)RimbodySettings.raceOption.Count * 24f);
            Widgets.BeginScrollView(outRect, ref rightScrollPosition, val);
            listing_Standard.Begin(val);
            foreach (RaceSetting item in RimbodySettings.raceOption.Values.OrderBy((RaceSetting x) => x.defName))
            {
                if (DefDatabase<ThingDef>.GetNamedSilentFail(item.defName) != null)
                {
                    string modName = item.modName;
                    listing_Standard.CheckboxLabeled(item.label.CapitalizeFirst(), ref item.isRimbodyEnabled, "From mod: " + modName);
                }

            }
            listing_Standard.End();
            Widgets.EndScrollView();
        }
    }
}
