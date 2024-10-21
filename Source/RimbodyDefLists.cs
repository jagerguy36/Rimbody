using System.Collections.Generic;
using Verse;

namespace Maux36.Rimbody
{
    //public class RimbodyDefLists
    //{
    //    public static List<ThingDef> WorkOutBuildings = new List<ThingDef>();
    //    WorkOutBuildings.

    //}

    public class RimbodyDefLists
    {
        public static List<ThingDef> StrengthBuilding = [];
        public static List<ThingDef> CardioBuilding = [];
        public static List<ThingDef> BalanceBuilding = [];
        public static List<ThingDef> WorkoutBuilding = [];

        static RimbodyDefLists() // Static constructor
        {

            if (ModsConfig.IsActive("kones.getrimped")){
                StrengthBuilding.Add(DefDatabase<ThingDef>.GetNamed("WeightBench", true));
                StrengthBuilding.Add(DefDatabase<ThingDef>.GetNamed("Barbell", true));
                StrengthBuilding.Add(DefDatabase<ThingDef>.GetNamed("PullupBars", true));
                StrengthBuilding.Add(DefDatabase<ThingDef>.GetNamed("SpinningDummy", true));

                BalanceBuilding.Add(DefDatabase<ThingDef>.GetNamed("BalanceBeam", true));
                BalanceBuilding.Add(DefDatabase<ThingDef>.GetNamed("YogaBall", true));

                CardioBuilding.Add(DefDatabase<ThingDef>.GetNamed("Treadmill", true));
                CardioBuilding.Add(DefDatabase<ThingDef>.GetNamed("ExerciseBike", true));

                WorkoutBuilding.AddRange(StrengthBuilding);
                WorkoutBuilding.AddRange(BalanceBuilding);
                WorkoutBuilding.AddRange(CardioBuilding);

            }
        }
    }

}
