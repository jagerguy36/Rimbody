using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Maux36.Rimbody
{
    [HarmonyLib.HarmonyPatch(typeof(Pawn))]
    internal static class DoorsPatches
    {
        [HarmonyLib.HarmonyPatch(nameof(Pawn.ExposeData)), HarmonyLib.HarmonyPostfix]
        internal static void ExposeDataPostfix(Pawn __instance)
        {
            Scribe_Values.Look(ref __instance.PawnBodyAngleOverride(), nameof(PawnExtensions.PawnBodyAngleOverride), -1);
        }
    }

    [HarmonyPatch(typeof(PawnRenderer), "BodyAngle")]
    public static class PawnRenderer_BodyAngle
    {
        public static bool Prefix(ref float __result, PawnRenderFlags flags, PawnRenderer __instance, Pawn ___pawn)
        {
            var overrideAngle = ___pawn.PawnBodyAngleOverride();
            if (overrideAngle >= 0 )
            {
                __result = overrideAngle;
                return false;
            }
            return true;
        }
    }
}
