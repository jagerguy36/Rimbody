using HarmonyLib;
using Verse;

namespace Maux36.Rimbody
{
    [HarmonyPatch(typeof(Pawn), "Notify_DisabledWorkTypesChanged")]
    public static class Pawn_Notify_DisabledWorkTypesChanged
    {
        static void Postfix(Pawn __instance)
        {
            var compPhysique = __instance.compPhysique();
            if (compPhysique != null)
            {
                compPhysique.DirtyTraitCache();
            }
        }
    }
}
