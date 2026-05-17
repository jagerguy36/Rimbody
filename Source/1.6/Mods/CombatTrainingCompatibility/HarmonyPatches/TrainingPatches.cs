using HarmonyLib;
using Maux36.Rimbody;
using System.Collections.Generic;
using Verse;

namespace Maux36.Rimbody_CombatTrainingCompatibility
{

    [HarmonyPatch(typeof(CompPhysique), "HarmonyCheck")]
    public class CompPhysique_HarmonyCheck_Patch
    {
        private static readonly int Key = RimbodyDB.HarmonyInjectorID["CombatTraining"];
        private static readonly int TrainOnCombatDummy_Key = DefDatabase<JobDef>.GetNamed("TrainOnCombatDummy").shortHash;
        public static bool Prefix(ref int __result, Pawn ___parent)
        {
            if (___parent?.jobs?.curJob?.def?.shortHash == TrainOnCombatDummy_Key && ___parent.jobs.curJob.verbToUse?.verbProps?.IsMeleeAttack == true && ___parent.pather?.MovingNow == false)
            {
                __result = Key;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CompPhysique), "HarmonyValues")]
    public class CompPhysique_HarmonyValues_Patch
    {
        private static readonly int Key = RimbodyDB.HarmonyInjectorID["CombatTraining"];
        public static bool Prefix(ref (float, float, List<float>) __result, int harmonyKey)
        {
            if (harmonyKey == Key)
            {
                __result = (1.2f, 0.6f, [0.5f, 0.5f, 0.5f, 0.3f, 0.2f, 0.2f, 0.1f, 0.1f, 0.1f]); //Melee
                return false;
            }
            return true;

        }
    }
}