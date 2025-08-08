using DubsBadHygiene;
using HarmonyLib;
using Maux36.Rimbody;
using Verse;

namespace Maux36.Rimbody_DBHCompatibility
{

    [HarmonyPatch(typeof(Need_Hygiene), "get_FallRateMulti21")]
    public static class Patch_Need_Hygiene_FallRateMulti21
    {
        static void Postfix(ref float __result, Pawn ___pawn)
        {
            __result *= PhysiqueMultiplier(___pawn);
        }

        private static float PhysiqueMultiplier(Pawn pawn)
        {
            var physiqueComp = pawn.compPhysique();
            if (physiqueComp != null )
            {
                if (physiqueComp.jobOverride)
                {
                    return 2f;
                }
            }
            return 1f;
        }
    }
}