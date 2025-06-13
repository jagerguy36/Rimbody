using HarmonyLib;
using CombatExtended;
using Verse;
using Maux36.Rimbody;

namespace Maux36.Rimbody_CE
{
    [HarmonyPatch(typeof(ThingOwner), "NotifyAdded")]
    public class ThingOwner_NotifyAdded_Patch
    {
        [HarmonyAfter(["ceteam.combatextended"])]
        public static void Postfix(ThingOwner __instance, Thing item)
        {
            if (item != null)
            {
                Rimbody_CE_Utility.TryUpdateWeight_CE(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(ThingOwner), "NotifyAddedAndMergedWith")]
    public class ThingOwner_NotifyAddedAndMergedWith_Patch
    {
        [HarmonyAfter(["ceteam.combatextended"])]
        public static void Postfix(ThingOwner __instance, Thing item, int mergedCount)
        {
            if (item != null && mergedCount != 0)
            {
                Rimbody_CE_Utility.TryUpdateWeight_CE(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(ThingOwner), "Take", [typeof(Thing), typeof(int)])]
    public class ThingOwner_Take_Patch
    {
        [HarmonyAfter(["ceteam.combatextended"])]
        public static void Postfix(ThingOwner __instance, Thing __result)
        {
            if (__result != null)
            {
                Rimbody_CE_Utility.TryUpdateWeight_CE(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(ThingOwner), "NotifyRemoved")]
    public class ThingOwner_NotifyRemoved_Patch
    {
        [HarmonyAfter(["ceteam.combatextended"])]
        public static void Postfix(ThingOwner __instance, Thing item)
        {
            if (item != null)
            {
                Rimbody_CE_Utility.TryUpdateWeight_CE(__instance);
            }

        }
    }
}
