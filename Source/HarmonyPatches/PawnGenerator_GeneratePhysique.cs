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
                if (compPhysique != null && compPhysique.BodyFat >= 0 && compPhysique.MuscleMass >= 0)
                {
                    __result = compPhysique.GetValidBody(pawn);
                }
            }
        }
    }
}