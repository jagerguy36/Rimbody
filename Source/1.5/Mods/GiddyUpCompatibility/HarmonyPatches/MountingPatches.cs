using GiddyUp;
using HarmonyLib;
using Maux36.Rimbody;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Muax36.Rimbody_GiddyUpCompatibility
{
    [HarmonyPatch(typeof(CompPhysique), "HarmonyCheck")]
    public class CompPhysique_HarmonyCheck_Patch
    {
        public static bool Prefix(ref string __result, CompPhysique __instance, Pawn ___parent)
        {
            if (___parent?.pather?.MovingNow == true && ExtendedDataStorage.isMounted.Contains(___parent.thingIDNumber))
            {
                __result = "giddyup_mounted";
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompPhysique), "HarmonyValues")]
    public class CompPhysique_HarmonyValues_Patch
    {
        public static bool Prefix(ref (float, float, List<float>) __result, string harmonyKey)
        {
            if (harmonyKey == "giddyup_mounted")
            {
                __result = (0.3f, 0.35f, null); //Activity
                return false;
            }
            return true;

        }
    }
}
