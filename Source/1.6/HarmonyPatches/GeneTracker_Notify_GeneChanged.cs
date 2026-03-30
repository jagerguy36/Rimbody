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
                if (compPhysique.PostGen)
                {
                    //For genes that should change bodytype immediately.
                    compPhysique.ResetBody();
                }
                else
                {
                    //During generation, recalculate body composition based on the genes.
                    compPhysique.PhysiqueValueSetup(true);
                }
            }        
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GenerateGenes")]
    public static class PawnGenerator_GenerateGenes
    {

        static void Postfix(Pawn pawn)
        {
            var compPhysique = pawn.compPhysique();
            if (compPhysique != null)
            {
                compPhysique.PostGen = true;
            }
        }
    }
}
