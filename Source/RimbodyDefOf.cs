using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Maux36.Rimbody
{

    [DefOf]
    public static class DefOf_Rimbody
    {
        public static JobDef Rimbody_Jogging;
        //public static JobDef Rimbody_GoDoPushUps;
        public static JobDef Rimbody_DoStrengthBuilding;
        public static JobDef Rimbody_DoBalanceBuilding;
        public static JobDef Rimbody_DoCardioBuilding;

        public static TimeAssignmentDef Rimbody_Workout;

        public static RoomRoleDef Rimbody_Gym;
        public static ThoughtDef WorkedOutInImpressiveGym;

        public static FleckDef Mote_Gain;

        static DefOf_Rimbody()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DefOf_Rimbody));
        }

    }
}