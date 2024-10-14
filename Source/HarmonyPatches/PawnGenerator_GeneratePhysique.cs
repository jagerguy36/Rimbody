using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Maux36.Rimbody
{
    [HarmonyPatch(typeof(PawnGenerator), "GenerateBodyType")]
    public static class PawnGenerator_GenerateBodyType
    {
        static void Postfix(Pawn pawn)
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            if (pawn != null && compPhysique != null)
            {
                pawn.BroadcastCompSignal("bodyTypeSelected for "+pawn.Name);
            }
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GetBodyTypeFor")]
    public static class PawnGenerator_GetBodyTypeFor
    {
        static void Postfix(ref BodyTypeDef __result, Pawn pawn)
        {
            if (!(ModsConfig.BiotechActive && pawn.DevelopmentalStage.Juvenile()))
            {
                var compPhysique = pawn.TryGetComp<CompPhysique>();
                if (compPhysique != null && compPhysique.BodyFat != -1 && compPhysique.MuscleMass != -1)
                {
                    __result = compPhysique.GetValidBody(pawn);
                }

                //TODO: set musclegain factor and fatgain factor to adjust for genes.
                //if (ModsConfig.BiotechActive && pawn.genes != null)
                //{
                //    List<Gene> genesListForReading = pawn.genes.GenesListForReading;
                //    for (int i = 0; i < genesListForReading.Count; i++)
                //    {
                //        if (genesListForReading[i].def.bodyType.HasValue)
                //        {
                //            tmpBodyTypes.Add(genesListForReading[i].def.bodyType.Value.ToBodyType(pawn));
                //        }
                //    }

                //    if (tmpBodyTypes.TryRandomElement(out var result))
                //    {
                //        return result;
                //    }
                //}

                //if (pawn.story.Adulthood != null)
                //{
                //    return pawn.story.Adulthood.BodyTypeFor(pawn.gender);
                //}
            }
        }
    }
}


