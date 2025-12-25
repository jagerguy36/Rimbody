using BigAndSmall;
using HarmonyLib;
using Maux36.Rimbody;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Maux36.Rimbody_BigAndSmall
{

    [HarmonyPatch(typeof(CompPropertiesMimicffect), "DoMimic")]
    public class MimicFix
    {
        public static void Postfix(Pawn pawn, Corpse corpse)
        {
            var compPhysique = pawn.compPhysique();
            var corpsePhysique = corpse.InnerPawn.TryGetComp<CompPhysique>();
            compPhysique.BodyFat = corpsePhysique.BodyFat;
            compPhysique.MuscleMass = corpsePhysique.MuscleMass;

        }
    }


    [HarmonyPatch(typeof(CompPhysique), "GetValidBody")]
    public class GetValidBodyPatch
    {
        public static void Postfix(CompPhysique __instance, ref BodyTypeDef __result)
        {
            var pawn = __instance.parent as Pawn;
            if (__result == BodyTypeDefOf.Male)
            {
                if (pawn != null && HumanoidPawnScaler.GetCache(pawn) is BSCache cache)
                {
                    Gender apparentGender = cache.GetApparentGender();
                    if (apparentGender == Gender.Female)
                    {
                        __result = BodyTypeDefOf.Female;
                    }
                }
            }

        }
    }

    [StaticConstructorOnStartup]
    public static class RimbodyCompFixer
    {
        static RimbodyCompFixer()
        {
            List<ThingDef> defToRemove = new();
            foreach (var def in DefDatabase<ThingDef>.AllDefs)
            {
                if (!PhysiqueCacheManager.TrackingDefHashSet.Contains(def.shortHash))
                    continue;
                if (def.GetRaceExtensions().SelectMany(x => x.PawnExtensionOnRace).Any(x => x.isMechanical))
                {
                    defToRemove.Add(def);
                }
            }
            foreach (var def in defToRemove)
            {
                var removed = def.comps.RemoveAll(c => c is CompProperties_Physique);
                if (removed > 0)
                {
                    Log.Message($"[Rimbody] Big and Small's mechanical Life form {def.defName} been excluded from tracking");
                    PhysiqueCacheManager.TrackingDefHashSet.Remove(def.shortHash);
                }
            }
        }
    }

}
