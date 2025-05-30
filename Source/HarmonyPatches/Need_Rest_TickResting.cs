using HarmonyLib;
using RimWorld;
using Verse;

namespace Maux36.Rimbody
{
    [HarmonyPatch(typeof(Need_Rest), "TickResting")]
    public static class Need_Rest_TickResting
    {
        static void Postfix(Need_Rest __instance, Pawn ___pawn, float restEffectiveness)
        {
            var compPhysique = ___pawn.compPhysique();
            if (compPhysique != null)
            {
                compPhysique.breInt = restEffectiveness;
            }
        }
    }
}
