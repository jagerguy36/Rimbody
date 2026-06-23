using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Maux36.Rimbody
{
    [HarmonyPatch(typeof(PawnGenerator), "TryGenerateNewPawnInternal")]
    public static class PawnGenerator_TryGenerateNewPawnInternal
    {
        static void Postfix(Pawn __result)
        {
            var compPhysique = __result?.compPhysique();
            if (compPhysique == null)
                return;
            compPhysique.PhysiqueValueSetup();
            compPhysique.PostGen = true;
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GetBodyTypeFor")]
    public static class PawnGenerator_GetBodyTypeFor
    {
        static bool Prefix(ref BodyTypeDef __result, Pawn pawn)
        {
            if (!(ModsConfig.BiotechActive && pawn.DevelopmentalStage.Juvenile()))
            {
                // This is for babies growing up.
                // compPhysique can be null when a pawn is generated.
                var compPhysique = pawn.compPhysique();
                if (compPhysique?.HasPhysique == true)
                {
                    __result = compPhysique.GetValidBody();
                    return false;
                }
            }
            return true;
        }
    }
}