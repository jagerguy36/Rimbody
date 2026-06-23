using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;

namespace Maux36.Rimbody
{

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
            if (RimbodyDB.ObservedGeneHash.Contains(addedOrRemovedGene.shortHash))
            {
                var compPhysique = ___pawn.compPhysique();
                if (compPhysique == null) return;
                compPhysique.NotifyActiveGeneCacheDirty();
                //For genes that should change bodytype immediately.
                compPhysique.ResetBody();
            }
        }
    }
}
