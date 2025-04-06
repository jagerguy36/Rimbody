using System.Collections.Generic;
using Verse;
using static UnityEngine.Scripting.GarbageCollector;

namespace Maux36.Rimbody
{
    [StaticConstructorOnStartup]
    public class RimbodyDefLists
    {
        public static Dictionary<ThingDef, ModExtensionRimbodyTarget> StrengthTarget = new();
        public static Dictionary<ThingDef, ModExtensionRimbodyTarget> CardioTarget = new();
        public static Dictionary<ThingDef, ModExtensionRimbodyTarget> BalanceTarget = new();
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
                    AddTarget(thingDef, buildingExtension);
                }
            }
        }

        private static void AddTarget(ThingDef targetDef, ModExtensionRimbodyTarget targetExtension)
        {
            switch (targetExtension.Category)
            {
                case RimbodyTargetCategory.Strength:
                    StrengthTarget[targetDef] = targetExtension;
                    break;
                case RimbodyTargetCategory.Balance:
                    BalanceTarget[targetDef] = targetExtension;
                    break;
                case RimbodyTargetCategory.Cardio:
                    CardioTarget[targetDef] = targetExtension;
                    break;
            }

            if(targetExtension.Type == RimbodyTargetType.Building)
            {
                WorkoutBuilding[targetDef] = targetExtension;
            }
            
        }
    }
}


