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
            var compPhysique = pawn.compPhysique();
            compPhysique?.PhysiqueValueSetup();
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