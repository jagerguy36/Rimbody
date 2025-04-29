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

    [HarmonyPatch(typeof(CompPhysique), "PhysiqueTick")]
    public class CompPhysique_PhysiqueTick_Patch
    {
        public static bool Prefix(CompPhysique __instance, float forcedCardio, float forcedStrength)
        {
            var pawn = __instance.parent as Pawn;
            if (pawn is not null && pawn.jobs?.curJob?.def?.defName== "TrainOnCombatDummy" && pawn.jobs.curJob.verbToUse?.verbProps?.IsMeleeAttack == true && pawn.pather?.MovingNow == false)
            {
                forcedCardio = 1.0f;
                forcedStrength = 1.2f;
                return true;
            }
            return true;

        }
    }
}