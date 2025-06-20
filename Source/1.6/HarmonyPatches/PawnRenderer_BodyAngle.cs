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
    [HarmonyPatch(typeof(PawnRenderer), "BodyAngle")]
    public static class PawnRenderer_BodyAngle
    {
        public static bool Prefix(ref float __result, PawnRenderFlags flags, PawnRenderer __instance, Pawn ___pawn)
        {
            var compPhysique = ___pawn.compPhysique();
            if (compPhysique?.pawnBodyAngleOverride >= 0 )
            {
                __result = compPhysique.pawnBodyAngleOverride;
                return false;
            }
            return true;
        }
    }
}
