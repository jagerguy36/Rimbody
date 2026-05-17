using GiddyUp;
using HarmonyLib;
using Maux36.Rimbody;
using System.Collections.Generic;
using Verse;

namespace Muax36.Rimbody_GiddyUpCompatibility
{
    [HarmonyPatch(typeof(CompPhysique), "HarmonyCheck")]
    public class CompPhysique_HarmonyCheck_Patch
    {
        private static readonly int Key = RimbodyDB.HarmonyInjectorID["GiddyUp"];
        public static bool Prefix(ref int __result, Pawn ___parent)
        {
            if (___parent?.pather?.MovingNow == true && ExtendedDataStorage.isMounted.Contains(___parent.thingIDNumber))
            {
                __result = Key;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompPhysique), "HarmonyValues")]
    public class CompPhysique_HarmonyValues_Patch
    {
        private static readonly int Key = RimbodyDB.HarmonyInjectorID["GiddyUp"];
        public static bool Prefix(ref (float, float, List<float>) __result, int harmonyKey)
        {
            if (harmonyKey == Key)
            {
                __result = (0.3f, 0.35f, null); //Activity
                return false;
            }
            return true;

        }
    }
}
