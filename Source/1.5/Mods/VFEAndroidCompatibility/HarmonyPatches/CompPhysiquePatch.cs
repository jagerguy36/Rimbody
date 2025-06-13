using HarmonyLib;
using System;
using System.Linq;
using Maux36.Rimbody;
using Verse;

namespace Muax36.Rimbody_VFEAndroidCompatibility
{

    [HarmonyPatch(typeof(CompPhysique), "PhysiqueValueSetup")]
    public class CompPhysique_PhysiqueValueSetup_Patch
    {
        public static bool Prefix(CompPhysique __instance, bool reset)
        {
            var pawn = __instance.parent as Pawn;
            if (pawn is not null && VREAndroids.Utils.IsAndroid(pawn) == true)
            {
                __instance.BodyFat = -2f;
                __instance.MuscleMass = -2f;
                return false;
            }
            return true;

        }
    }
}