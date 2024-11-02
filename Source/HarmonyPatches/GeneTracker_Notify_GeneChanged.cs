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

        static void Postfix(Pawn_GeneTracker __instance, GeneDef addedOrRemovedGene)
        {
            var pawnField = typeof(Pawn_GeneTracker).GetField("pawn");
            var pawn = (Pawn)pawnField.GetValue(__instance);

            if (ModsConfig.BiotechActive && pawn?.genes != null && addedOrRemovedGene.bodyType.HasValue)
            {
                var compPhysique = pawn.TryGetComp<CompPhysique>();

                if (compPhysique != null)
                {
                    if (compPhysique.PostGen)
                    {
                        compPhysique.ApplyGene();
                        compPhysique.ResetBody();
                    }
                    else
                    {
                        compPhysique.ApplyGene();
                        (compPhysique.BodyFat, compPhysique.MuscleMass) = compPhysique.RandomCompPhysiqueByBodyType();
                    }
                }
            }            
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GenerateGenes")]
    public static class PawnGenerator_GenerateGenes
    {

        static void Postfix(Pawn pawn)
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            compPhysique.PostGen = true;

        }
    }
}
