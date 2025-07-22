using RimWorld;
using Verse;

namespace Maux36.Rimbody
{

    [DefOf]
    public static class DefOf_Rimbody
    {
        public static JobDef Rimbody_Jogging;
        public static JobDef Rimbody_DoStrengthPushUps;
        public static JobDef Rimbody_DoChunkLifting;
        public static JobDef Rimbody_DoChunkOverheadPress;
        public static JobDef Rimbody_DoChunkSquats;
        public static JobDef Rimbody_DoStrengthLifting;
        public static JobDef Rimbody_DoBalanceLifting;
        public static JobDef Rimbody_DoStrengthBuilding;
        public static JobDef Rimbody_DoBalanceBuilding;
        public static JobDef Rimbody_DoBodyWeightPlank;
        public static JobDef Rimbody_DoCardioBuilding;
        public static JobDef Rimbody_RecoverWander;
        public static JobDef Rimbody_RecoverWait;

        public static StatDef Rimbody_WorkoutEfficiency;

        public static TimeAssignmentDef Rimbody_Workout;

        public static RoomRoleDef Rimbody_Gym;
        public static ThoughtDef WorkedOutInImpressiveGym;

        public static FleckDef Mote_MaxGain;
        public static FleckDef Mote_Gain;
        public static FleckDef Mote_GainLimited;
        public static FleckDef Mote_Cardio;
        public static FleckDef Mote_CardioLimited;
        public static FleckDef Mote_Rimbody_Plank;

        public static JoyKindDef Rimbody_WorkoutJoy;

        public static ThoughtDef Rimbody_RunnerHigh;
        public static ThoughtDef Rimbody_GoodRun;

        public static ThingDef Rimbody_FlatBench;
        public static ThingDef Rimbody_ExerciseMat;

        static DefOf_Rimbody()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DefOf_Rimbody));
        }

    }
}