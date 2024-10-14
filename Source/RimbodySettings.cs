using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Maux36.Rimbody
{
    internal class RimbodySettings : ModSettings
    {
        public static int RecoveryTick = 200;
        public static float fatThresholdFat = 35f;
        public static float fatThresholdThin = 15f;
        public static float muscleThresholdHulk = 35f;
        public static float muscleThresholdThin = 15f;
        public static float gracePeriod = 1f;
        public static string[] gainingJobs = ["Mining"];
        public static float rateFactor = 1f;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref fatThresholdFat, "Rimbody_fatThresholdFat", 30f, true);
            Scribe_Values.Look(ref fatThresholdThin, "Rimbody_fatThresholdThin", 15f, true);
            Scribe_Values.Look(ref muscleThresholdHulk, "Rimbody_muscleThresholdHulk", 30f, true);
            Scribe_Values.Look(ref muscleThresholdThin, "Rimbody_muscleThresholdThin", 15f, true);
            Scribe_Values.Look(ref gracePeriod, "Rimbody_gracePeriod", 2.5f, true);
        }
    }
}
