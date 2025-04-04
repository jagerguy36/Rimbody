using System.Collections.Generic;
using Verse;
using static UnityEngine.Scripting.GarbageCollector;

namespace Maux36.Rimbody
{
    [StaticConstructorOnStartup]
    public class RimbodyDefLists
    {
        public static Dictionary<ThingDef, ModExtensionRimbodyTarget> StrengthBuilding = new();
        public static Dictionary<ThingDef, ModExtensionRimbodyTarget> CardioBuilding = new();
        public static Dictionary<ThingDef, ModExtensionRimbodyTarget> BalanceBuilding = new();
        public static Dictionary<ThingDef, ModExtensionRimbodyTarget> WorkoutBuilding = new();
        //public static List<ThingDef> ChunkList = new();

        static RimbodyDefLists() // Static constructor
        {
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                //if (thingDef.thingCategories != null && thingDef.thingCategories.Contains(DefDatabase<ThingCategoryDef>.GetNamed("StoneChunks", true)))
                //{
                //    ChunkList.Add(thingDef);
                //}
                var buildingExtension = thingDef.GetModExtension<ModExtensionRimbodyTarget>();
                if (buildingExtension != null)
                {
                    AddBuilding(thingDef, buildingExtension);
                }
            }
        }

        private static void AddBuilding(ThingDef buildingDef, ModExtensionRimbodyTarget buildingExtension)
        {
            switch (buildingExtension.Category)
            {
                case RimbodyTargetCategory.Strength:
                    StrengthBuilding[buildingDef] = buildingExtension;
                    break;
                case RimbodyTargetCategory.Balance:
                    BalanceBuilding[buildingDef] = buildingExtension;
                    break;
                case RimbodyTargetCategory.Cardio:
                    CardioBuilding[buildingDef] = buildingExtension;
                    break;
            }

            WorkoutBuilding[buildingDef] = buildingExtension;
        }
    }
}


