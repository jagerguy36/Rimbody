using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;


namespace Maux36.Rimbody
{
    [HarmonyPatchCategory("NonCE")]
    [HarmonyPatch(typeof(ThingOwner), "NotifyAdded")]
    public class ThingOwner_NotifyAdded_Patch
    {
        public static void Postfix(ThingOwner __instance, Thing item)
        {
            if (item != null)
            {
                Rimbody_Utility.TryUpdateWeight(__instance);
            }
        }
    }

    [HarmonyPatchCategory("NonCE")]
    [HarmonyPatch(typeof(ThingOwner), "NotifyAddedAndMergedWith")]
    public class ThingOwner_NotifyAddedAndMergedWith_Patch
    {
        public static void Postfix(ThingOwner __instance, Thing item, int mergedCount)
        {
            if (item != null && mergedCount != 0)
            {
                Rimbody_Utility.TryUpdateWeight(__instance);
            }
        }
    }

    [HarmonyPatchCategory("NonCE")]
    [HarmonyPatch(typeof(ThingOwner), "Take", [typeof(Thing), typeof(int)])]
    public class ThingOwner_Take_Patch
    {
        public static void Postfix(ThingOwner __instance, Thing __result)
        {
            if (__result != null)
            {
                Rimbody_Utility.TryUpdateWeight(__instance);
            }
        }
    }

    [HarmonyPatchCategory("NonCE")]
    [HarmonyPatch(typeof(ThingOwner), "NotifyRemoved")]
    public class ThingOwner_NotifyRemoved_Patch
    {
        public static void Postfix(ThingOwner __instance, Thing item)
        {
            if (item != null)
            {
                Rimbody_Utility.TryUpdateWeight(__instance);
            }

        }
    }
}
