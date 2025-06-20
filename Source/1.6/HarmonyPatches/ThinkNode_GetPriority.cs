using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Maux36.Rimbody
{
    [HarmonyPatch]
    public class JobGiver_GetPriority
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method("JobGiver_GetRest:GetPriority", [typeof(Pawn)]);
            yield return AccessTools.Method("ThinkNode_Priority_GetJoy:GetPriority", [typeof(Pawn)]);
        }

        [UsedImplicitly]
        private static bool Prefix(Pawn pawn, ref float __result)
        {
            if (pawn?.timetable?.CurrentAssignment == DefOf_Rimbody.Rimbody_Workout)
            {
                __result = 0f;
                return false;
            }

            return true;
        }
    }


    [HarmonyPatch(typeof(JobGiver_Work), "GetPriority")]
    public class WorkPriorityPatch
    {
        public static bool Prefix(Pawn pawn, ref float __result)
        {
            if (pawn == null)
            {
                return true;
            }
            if (pawn.workSettings == null || !pawn.workSettings.EverWork)
            {
                __result = 0f;
                return false;
            }
            TimeAssignmentDef timeAssignmentDef = ((pawn.timetable == null) ? TimeAssignmentDefOf.Anything : pawn.timetable.CurrentAssignment);
            if (timeAssignmentDef == DefOf_Rimbody.Rimbody_Workout)
            {
                __result = 2f;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(JobGiver_GetFood), "GetPriority")]
    public class GetRestPriorityPatch
    {
        public static bool Prefix(Pawn pawn, ref float __result)
        {
            if (pawn == null)
            {
                return true;
            }

            var compPhysique = pawn.compPhysique();

            if (compPhysique?.useFatgoal == true && compPhysique.BodyFat > compPhysique.FatGoal) //Fat goal not satisfied
            {

                //If Both not satisfied
                if (compPhysique?.useMuscleGoal == true && compPhysique.MuscleMass < compPhysique.MuscleGoal)
                {
                    return true; //return normal behavior
                }

                Need_Food food = pawn.needs.food;
                //Muscle goal is satisfied: only fat goal matters
                if ((int)food.CurCategory < (int)HungerCategory.UrgentlyHungry)
                {
                    __result = 0f;
                    return false;
                }
                else
                {
                    return true;
                }
            }

            //Fat goal is satisfied already
            else if (compPhysique?.useMuscleGoal == true && compPhysique.MuscleMass < compPhysique.MuscleGoal) //Fat goal satisfied and Muscle goal not satisfied
            {
                Need_Food food = pawn.needs.food;
                if (food.CurLevelPercentage < 0.5f)
                {
                    __result = 9.5f;
                    return false;
                }
                else
                {
                    return true;
                }
            }

            //Both satisfied
            return true;
        }
    }
}
