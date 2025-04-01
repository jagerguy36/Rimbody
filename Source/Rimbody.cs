using System;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using static System.Net.Mime.MediaTypeNames;

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

            if (ModsConfig.IsActive("mlie.syrindividuality"))
            {
                IndividualityLoaded = true;
            }

            if (ModsConfig.IsActive("divinederivative.romance"))
            {
                WayBetterRomanceLoaded = true;
            }

            if (ModsConfig.IsActive("aoba.exosuit.framework"))
            {
                ExosuitFrameworkLoaded = true;
            }

            if (ModsConfig.IsActive("ceteam.combatextended"))
            {
                CombatExtendedLoaded = true;
            }
        }

        public override string SettingsCategory()
        {
            return "RimbodySettingCategory".Translate();
        }

        private static Vector2 scrollPosition = new Vector2(0f, 0f);
        private static float totalContentHeight = ModsConfig.BiotechActive?770f:720f;
        private const float ScrollBarWidthMargin = 18f;
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect outerRect = inRect.ContractedBy(10f);
            bool scrollBarVisible = totalContentHeight > outerRect.height;
            var scrollViewTotal = new Rect(0f, 0f, outerRect.width - (scrollBarVisible ? ScrollBarWidthMargin : 0), totalContentHeight);
            Widgets.BeginScrollView(outerRect, ref scrollPosition, scrollViewTotal);

            var listing_Standard = new Listing_Standard();
            listing_Standard.Begin(new Rect(0f, 0f, scrollViewTotal.width, 9999f));
            listing_Standard.Gap(12f);

            listing_Standard.Label("RimbodyGeneralSetting".Translate());
            listing_Standard.Gap(12f);
            RimbodySettings.rateFactor = (float)Math.Round(listing_Standard.SliderLabeled("RimbodyRateFactor".Translate() + " ("+"Default".Translate()+" 1): " + RimbodySettings.rateFactor, RimbodySettings.rateFactor, 0.1f, 5f, tooltip: "RimbodyRateFactorTooltip".Translate()), 1);
            listing_Standard.Gap(6f);
            RimbodySettings.WorkOutGainEfficiency = (float)Math.Round(listing_Standard.SliderLabeled("RimbodyWorkOutGainEfficiency".Translate() + " (" + "Default".Translate() + " 1.0): " + RimbodySettings.WorkOutGainEfficiency, RimbodySettings.WorkOutGainEfficiency, 1f, 2f, tooltip: "RimbodyWorkOutGainEfficiencyTooltip".Translate()), 1);
            listing_Standard.Gap(6f);
            RimbodySettings.carryRateMultiplier = Mathf.Clamp((float)Math.Round(listing_Standard.SliderLabeled("RimbodyCarryRateMultiplier".Translate() + " (" + "Default".Translate() + " 1.0): " + RimbodySettings.carryRateMultiplier, RimbodySettings.carryRateMultiplier, 0f, 1f, tooltip: "RimbodyCarryRateMultiplierTooltip".Translate()), 1), 0f, 1f);
            listing_Standard.Gap(6f);
            listing_Standard.CheckboxLabeled("RimbodyGenderDifference".Translate(), ref RimbodySettings.genderDifference, "RimbodyGenderDifferenceTooltip".Translate());
            listing_Standard.Gap(6f);
            listing_Standard.CheckboxLabeled("RimbodyShowFleck".Translate(), ref RimbodySettings.showFleck, "RimbodyShowFleckTooltip".Translate());
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
            RimbodySettings.fatThresholdFat = (float)Math.Round(listing_Standard.SliderLabeled("RimbodyFatThreshholdFat".Translate()+ " (" + "Default".Translate() + " 35): " + RimbodySettings.fatThresholdFat, RimbodySettings.fatThresholdFat, 25, 50, tooltip: "RimbodyFatThreshholdFatTooltip".Translate()),1);
            RimbodySettings.fatThresholdThin = (float)Math.Round(listing_Standard.SliderLabeled("RimbodyFatThresholdThin".Translate() + " (" + "Default".Translate() + " 15): " + RimbodySettings.fatThresholdThin, RimbodySettings.fatThresholdThin, 0, 25, tooltip: "RimbodyFatThresholdThinTooltip".Translate()), 1);
            listing_Standard.Gap(6f);
            RimbodySettings.muscleThresholdHulk = (float)Math.Round(listing_Standard.SliderLabeled("RimbodyMuscleThresholdHulk".Translate() + " (" + "Default".Translate() + " 35): " + RimbodySettings.muscleThresholdHulk, RimbodySettings.muscleThresholdHulk, 25, 50, tooltip: "RimbodyMuscleThresholdHulkTooltip".Translate()), 1);
            RimbodySettings.muscleThresholdThin = (float)Math.Round(listing_Standard.SliderLabeled("RimbodyMuscleThresholdThin".Translate() + " (" + "Default".Translate() + " 15): " + RimbodySettings.muscleThresholdThin, RimbodySettings.muscleThresholdThin, 0, 25, tooltip: "RimbodyMuscleThresholdThinTooltip".Translate()), 1);
            listing_Standard.Gap(6f);
            RimbodySettings.gracePeriod = (float)Math.Round(listing_Standard.SliderLabeled("RimbodyGracePeriod".Translate() + " (" + "Default".Translate() + " 1): " + RimbodySettings.gracePeriod, RimbodySettings.gracePeriod, 0.1f, 5, tooltip: "RimbodyGracePeriodTooltip".Translate()), 1);

            if (ModsConfig.BiotechActive)
            {
                listing_Standard.Gap(24f);
                RimbodySettings.nonSenescentpoint = (int)Math.Round(listing_Standard.SliderLabeled("RimbodyNonSenescentpoint".Translate() + " (" + "Default".Translate() + " 25): " + RimbodySettings.nonSenescentpoint, RimbodySettings.nonSenescentpoint, 13, 60, tooltip: "RimbodyNonSenescentpointTooltip".Translate()), 0);
            }

            listing_Standard.Gap(24f);
            if (listing_Standard.ButtonText("RimbodyDefaultSetting".Translate(), "RimbodyDefaultSettingTooltip".Translate()))
            {
                RimbodySettings.rateFactor = 1f;
                RimbodySettings.CalcEveryTick = 150;
                RimbodySettings.genderDifference = true;
                RimbodySettings.showFleck = true;
                RimbodySettings.fatThresholdFat = 35f;
                RimbodySettings.fatThresholdThin = 15f;
                RimbodySettings.muscleThresholdHulk = 35f;
                RimbodySettings.muscleThresholdThin = 15f;
                RimbodySettings.gracePeriod = 1f;
                RimbodySettings.nonSenescentpoint = 25;
                RimbodySettings.WorkOutGainEfficiency = 1.0f;
            }

            listing_Standard.End();
            Widgets.EndScrollView();
        }

    }

}
