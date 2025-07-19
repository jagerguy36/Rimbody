using HarmonyLib;
using Maux36.Rimbody;
using System.Collections.Generic;
using Verse;

namespace Maux36.Rimbody_CombatTrainingCompatibility
{

    [HarmonyPatch(typeof(CompPhysique), "HarmonyCheck")]
    public class CompPhysique_HarmonyCheck_Patch
    {
        public static bool Prefix(ref string __result, Pawn ___parent)
        {
            if (___parent?.jobs?.curJob?.def?.defName == "TrainOnCombatDummy" && ___parent.jobs.curJob.verbToUse?.verbProps?.IsMeleeAttack == true && ___parent.pather?.MovingNow == false)
            {
                __result = "combat_training";
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompPhysique), "HarmonyValues")]
    public class CompPhysique_HarmonyValues_Patch
    {
        public static bool Prefix(ref (float, float, List<float>) __result, string harmonyKey)
        {
            if (harmonyKey == "combat_training")
            {
                __result = (1.2f, 0.6f, [0.5f, 0.5f, 0.5f, 0.3f, 0.2f, 0.2f, 0.1f, 0.1f, 0.1f]); //Melee
                return false;
            }
            return true;

        }
    }
}