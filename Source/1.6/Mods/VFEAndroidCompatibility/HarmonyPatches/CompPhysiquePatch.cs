using HarmonyLib;
using Maux36.Rimbody;
using RimWorld;
using Verse;
using VREAndroids;

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


    [HarmonyPatch(typeof(Pawn_GeneTracker), "Notify_GenesChanged")]
    public static class GeneTracker_Notify_GeneChanged
    {
        public static bool Prepare()
        {
            if (ModsConfig.BiotechActive)
                return true;
            return false;
        }
        static void Postfix(Pawn_GeneTracker __instance, GeneDef addedOrRemovedGene, Pawn ___pawn)
        {
            if (addedOrRemovedGene is AndroidGeneDef androidGeneDef && androidGeneDef.isCoreComponent)
            {
                var compPhysique = ___pawn.compPhysique();
                if (compPhysique?.HasPhysique != true) return;
                compPhysique.NotifyActiveGeneCacheDirty();
                compPhysique.PhysiqueValueSetup(true);
            }
        }
    }
}