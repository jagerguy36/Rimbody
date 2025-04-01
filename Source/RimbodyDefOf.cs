﻿using RimWorld;
using Verse;

namespace Maux36.Rimbody
{

    [DefOf]
    public static class DefOf_Rimbody
    {
        public static JobDef Rimbody_Jogging;
        public static JobDef Rimbody_DoStoneLifting;
        //public static JobDef Rimbody_GoDoPushUps;
        public static JobDef Rimbody_DoStrengthBuilding;
        public static JobDef Rimbody_DoBalanceBuilding;
        public static JobDef Rimbody_DoCardioBuilding;

        public static TimeAssignmentDef Rimbody_Workout;

        public static RoomRoleDef Rimbody_Gym;
        public static ThoughtDef WorkedOutInImpressiveGym;

        public static FleckDef Mote_MaxGain;
        public static FleckDef Mote_Gain;
        public static FleckDef Mote_GainLimited;
        public static FleckDef Mote_Cardio;
        public static FleckDef Mote_CardioLimited;

        public static JoyKindDef Rimbody_WorkoutJoy;

        public static ThoughtDef Rimbody_RunnerHigh;
        public static ThoughtDef Rimbody_GoodRun;

        static DefOf_Rimbody()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DefOf_Rimbody));
        }

    }
}