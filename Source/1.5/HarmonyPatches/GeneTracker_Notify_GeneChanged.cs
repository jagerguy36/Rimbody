using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
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
        static void Postfix(Pawn_GeneTracker __instance, Pawn ___pawn, GeneDef addedOrRemovedGene)
        {
            if (___pawn?.genes == null) return;
            if (addedOrRemovedGene.def == DefOf_Rimbody.DiseaseFree)
            {
                var compPhysique = ___pawn.compPhysique();
                compPhysique?.ApplyGene();
            }

            else if (addedOrRemovedGene.bodyType.HasValue)
            {
                var compPhysique = ___pawn.compPhysique();
                if (compPhysique == null) return;
                compPhysique.ApplyGene();
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
            compPhysique?.PostGen = true;
        }
    }
}
