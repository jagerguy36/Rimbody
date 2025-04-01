using System.Collections.Generic;
using Verse;
using static UnityEngine.Scripting.GarbageCollector;

namespace Maux36.Rimbody
{
    public class RimbodyDefLists
    {
        public static Dictionary<ThingDef, ModExtentionRimbodyBuilding> StrengthBuilding = new();
        public static Dictionary<ThingDef, ModExtentionRimbodyBuilding> CardioBuilding = new();
        public static Dictionary<ThingDef, ModExtentionRimbodyBuilding> BalanceBuilding = new();
        public static Dictionary<ThingDef, ModExtentionRimbodyBuilding> WorkoutBuilding = new();
        public static List<ThingDef> StoneChunkList = new();

        static RimbodyDefLists() // Static constructor
        {
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (thingDef.thingCategories != null && thingDef.thingCategories.Contains(DefDatabase<ThingCategoryDef>.GetNamed("StoneChunks", true)))
                {
                    StoneChunkList.Add(thingDef);
                }
                var buildingExtension = thingDef.GetModExtension<ModExtentionRimbodyBuilding>();
                if (buildingExtension != null)
                {
                    AddBuilding(thingDef, buildingExtension);
                }
            }
        }

        private static void AddBuilding(ThingDef buildingDef, ModExtentionRimbodyBuilding buildingExtension)
        {
            switch (buildingExtension.type)
            {
                case RimbodyBuildingType.Strength:
                    StrengthBuilding[buildingDef] = buildingExtension;
                    break;
                case RimbodyBuildingType.Balance:
                    BalanceBuilding[buildingDef] = buildingExtension;
                    break;
                case RimbodyBuildingType.Cardio:
                    CardioBuilding[buildingDef] = buildingExtension;
                    break;
            }

            WorkoutBuilding[buildingDef] = buildingExtension;
        }
    }
}


