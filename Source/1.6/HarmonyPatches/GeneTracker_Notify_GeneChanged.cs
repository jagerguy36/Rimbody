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
            if (RimbodyDB.GeneFactors.Keys.Contains(addedOrRemovedGene.shortHash) || addedOrRemovedGene.def == DefOf_Rimbody.DiseaseFree)
            {
                var compPhysique = ___pawn.compPhysique();
                if (compPhysique == null) return;
                compPhysique.NotifyActiveGeneCacheDirty();
                if (compPhysique.PostGen)
                {
                    compPhysique.ResetBody();
                }
                else
                {
                    (compPhysique.BodyFat, compPhysique.MuscleMass) = compPhysique.RandomCompPhysiqueByBodyType();
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
