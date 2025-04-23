using Verse;

namespace Maux36.Rimbody
{
    public class RimbodySettings : ModSettings
    {
        public const int PartCount = 9;
        public const int MaxFatiguePerPart = 10;

        public static int CalcEveryTick = 150;
        public static int RecoveryTick = 250;
        public static float fatThresholdFat = 35f;
        public static float fatThresholdThin = 15f;
        public static float muscleThresholdHulk = 35f;
        public static float muscleThresholdThin = 15f;
        public static float gracePeriod = 1f;
        public static float rateFactor = 1f;
        public static int nonSenescentpoint = 25;
        public static float maleMusclegain = 0.015f;
        public static float femaleFatThreshold = 1.0f;
        
        public static bool genderDifference = true;
        public static bool showFleck = true;
        public static bool useFatigue = true;
        public static bool useExhaustion = false;

        public static float WorkOutGainEfficiency = 1.0f;
        public static float carryRateMultiplier = 1.0f;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref CalcEveryTick, "Rimbody_CalcEveryTick", 150, true);
            //Scribe_Values.Look(ref RecoveryTick, "Rimbody_RecoveryTick", 200, true);
            Scribe_Values.Look(ref fatThresholdFat, "Rimbody_fatThresholdFat", 30f, true);
            Scribe_Values.Look(ref fatThresholdThin, "Rimbody_fatThresholdThin", 15f, true);
            Scribe_Values.Look(ref muscleThresholdHulk, "Rimbody_muscleThresholdHulk", 30f, true);
            Scribe_Values.Look(ref muscleThresholdThin, "Rimbody_muscleThresholdThin", 15f, true);
            Scribe_Values.Look(ref gracePeriod, "Rimbody_gracePeriod", 2.5f, true);
            Scribe_Values.Look(ref rateFactor, "Rimbody_rateFactor", 1f, true);
            Scribe_Values.Look(ref nonSenescentpoint, "Rimbody_nonSenescentpoint", 25, true);
            Scribe_Values.Look(ref maleMusclegain, "Rimbody_maleMusclegain", 0.1f, true);
            Scribe_Values.Look(ref femaleFatThreshold, "Rimbody_femaleFatThreshold", 1.5f, true);
            Scribe_Values.Look(ref genderDifference, "Rimbody_genderDifference", true, true);
            Scribe_Values.Look(ref showFleck, "Rimbody_showFleck", true, true);
            Scribe_Values.Look(ref useFatigue, "Rimbody_useFatigue", true, true);
            Scribe_Values.Look(ref useExhaustion, "Rimbody_useExhaustion", false, true);
            Scribe_Values.Look(ref WorkOutGainEfficiency, "Rimbody_WorkOutGainEfficiency", 1.0f, true);
            Scribe_Values.Look(ref carryRateMultiplier, "Rimbody_carryRateMultiplier", 1.0f, true);
        }
    }
}

