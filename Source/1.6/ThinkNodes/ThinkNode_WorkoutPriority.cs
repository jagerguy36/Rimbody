using Verse.AI;
using Verse;
using RimWorld;
using System;

namespace Maux36.Rimbody
{
    internal class ThinkNode_WorkoutPriority : ThinkNode_Priority
    {
        public override float GetPriority(Pawn pawn)
        {
            if (pawn != null && pawn.ageTracker?.CurLifeStage?.developmentalStage == DevelopmentalStage.Adult)
            {
                if (pawn.Downed || pawn.Drafted) return 0f;
                if (Rimbody_Utility.TooTired(pawn)) return 0f;
                TimeAssignmentDef timeAssignmentDef = ((pawn.timetable == null) ? TimeAssignmentDefOf.Anything : pawn.timetable.CurrentAssignment);
                //Workout schedule
                if (timeAssignmentDef == DefOf_Rimbody.Rimbody_Workout)
                {
                    var compPhysique = pawn.compPhysique();
                    if (compPhysique == null)
                    {
                        return 0;
                    }
                    if (RimbodySettings.useExhaustion && compPhysique.resting)
                    {
                        return 0f;
                    }
                    if (HealthAIUtility.ShouldSeekMedicalRest(pawn))
                    {
                        return 0f;
                    }
                    return 9f;
                }
            }
            return 0f;
        }
    }
}
