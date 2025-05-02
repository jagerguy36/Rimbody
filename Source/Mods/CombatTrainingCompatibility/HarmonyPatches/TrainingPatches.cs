using HarmonyLib;
using Maux36.Rimbody;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Maux36.Rimbody_CombatTrainingCompatibility
{

    //[HarmonyPatch(typeof(CompPhysique), "PhysiqueTick")]
    //public class CompPhysique_PhysiqueTick_Patch
    //{
    //    public static bool Prefix(CompPhysique __instance, float forcedCardio, float forcedStrength)
    //    {
    //        var pawn = __instance.parent as Pawn;
    //        if (pawn is not null && pawn.jobs?.curJob?.def?.defName== "TrainOnCombatDummy" && pawn.jobs.curJob.verbToUse?.verbProps?.IsMeleeAttack == true && pawn.pather?.MovingNow == false)
    //        {
    //            forcedCardio = 1.0f;
    //            forcedStrength = 1.2f;
    //            return true;
    //        }
    //        return true;

    //    }
    //}

    [HarmonyPatch(typeof(CompPhysique), "HarmonyCheck")]
    public class CompPhysique_HarmonyCheck_Patch
    {
        public static bool Prefix(ref string __result, CompPhysique __instance, Pawn ___parent)
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
                __result = (1.2f, 0.4f, [0.5f, 0.5f, 0.5f, 0.3f, 0.2f, 0.2f, 0.1f, 0.1f, 0.1f]);
                return false;
            }
            return true;

        }
    }
}