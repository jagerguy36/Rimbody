﻿using System;
using System.Reflection;
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
        }

        public override string SettingsCategory()
        {
            return "RimbodySettingCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing_Standard = new Listing_Standard();
            listing_Standard.Begin(inRect);
            listing_Standard.Gap(18f);
            listing_Standard.Label("RimbodyGeneralSetting".Translate());
            listing_Standard.Gap(6f);
            RimbodySettings.rateFactor = (float)Math.Round(listing_Standard.SliderLabeled("RimbodyRateFactor".Translate() + " ("+"Default".Translate()+" 1): " + RimbodySettings.rateFactor, RimbodySettings.rateFactor, 0.1f, 5f, tooltip: "RimbodyRateFactorTooltip".Translate()), 1);
            listing_Standard.Gap(3f);
            listing_Standard.CheckboxLabeled("RimbodyGenderDifference".Translate(), ref RimbodySettings.genderDifference, "RimbodyGenderDifferenceTooltip".Translate());
            listing_Standard.Gap(3f);
            listing_Standard.CheckboxLabeled("RimbodyShowFleck".Translate(), ref RimbodySettings.showFleck, "RimbodyShowFleckTooltip".Translate());
            listing_Standard.Gap(24f);
            listing_Standard.Label("RimbodyThreshholdSetting".Translate());
            listing_Standard.Gap(6f);
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
                RimbodySettings.fatThresholdFat = 35f;
                RimbodySettings.fatThresholdThin = 15f;
                RimbodySettings.muscleThresholdHulk = 35f;
                RimbodySettings.muscleThresholdThin = 15f;
                RimbodySettings.gracePeriod = 1f;
                RimbodySettings.nonSenescentpoint = 25;
            }

            listing_Standard.End();
        }

    }

}
