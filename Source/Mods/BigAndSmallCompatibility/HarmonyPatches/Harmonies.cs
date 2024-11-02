using BigAndSmall;
using HarmonyLib;
using Maux36.Rimbody;
using RimWorld;
using Verse;

namespace Maux36.Rimbody_BigAndSmall
{

    [HarmonyPatch(typeof(CompPropertiesMimicffect), "DoMimic")]
    public class MimicFix
    {
        public static void Postfix(Pawn pawn, Corpse corpse)
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            var corpsePhysique = corpse.InnerPawn.TryGetComp<CompPhysique>();
            compPhysique.BodyFat = corpsePhysique.BodyFat;
            compPhysique.MuscleMass = corpsePhysique.MuscleMass;

        }
    }


    [HarmonyPatch(typeof(CompPhysique), "GetValidBody")]
    public class GetValidBodyPatch
    {
        public static void Postfix(ref BodyTypeDef __result, Pawn pawn)
        {
            if (pawn != null && pawn.gender == Gender.Male && pawn?.genes?.GenesListForReading?.Any(x => x.def == BSDefs.Body_Androgynous) == true)
            {
                if (__result == BodyTypeDefOf.Male)
                {
                    __result = BodyTypeDefOf.Female;
                }
            }

        }
    }

}
