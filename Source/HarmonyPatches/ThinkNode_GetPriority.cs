using HarmonyLib;
using JetBrains.Annotations;
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
            yield return AccessTools.Method("JobGiver_Work:GetPriority", [typeof(Pawn)]);
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
}
