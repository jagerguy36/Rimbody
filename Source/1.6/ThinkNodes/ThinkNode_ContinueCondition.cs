using Verse.AI;
using Verse;
using RimWorld;
using System;

namespace Maux36.Rimbody
{
    internal class ThinkNode_ContinueCondition : ThinkNode_Conditional //Satisfy goal when Idle
    {
        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn != null && pawn.ageTracker?.CurLifeStage?.developmentalStage == DevelopmentalStage.Adult)
            {
                if (pawn.Downed || pawn.Drafted) return false;
                if (Rimbody_Utility.TooTired(pawn)) return false;
                TimeAssignmentDef timeAssignmentDef = ((pawn.timetable == null) ? TimeAssignmentDefOf.Anything : pawn.timetable.CurrentAssignment);
                var compPhysique = pawn.compPhysique();
                if (compPhysique == null)
                {
                    return false;
                }
                //Workout schedule

                if (HealthAIUtility.ShouldSeekMedicalRest(pawn))
                {
                    return false;
                }
                //For joy
                if (!timeAssignmentDef.allowJoy)
                {
                    return false;
                }
                if (pawn.needs.joy.tolerances.BoredOf(DefOf_Rimbody.Rimbody_WorkoutJoy))
                {
                    return false;
                }
                bool continuousWorkoutCheck = false;
                if (compPhysique.AssignedTick > 0)
                {
                    if (Find.TickManager.TicksGame - compPhysique.lastWorkoutTick < RimbodySettings.RecoveryTick * 4f)
                    {
                        Log.Message($"Assigned Tick : {compPhysique.AssignedTick} and continuing");
                        continuousWorkoutCheck = true;
                    }
                    else
                    {
                        Log.Message($"Assigned Tick : {compPhysique.AssignedTick} and ignoring");
                        compPhysique.AssignedTick = 0;
                    }
                }
                if (timeAssignmentDef == TimeAssignmentDefOf.Anything)
                {
                    return continuousWorkoutCheck;
                }
                if (timeAssignmentDef == TimeAssignmentDefOf.Joy)
                {
                    return continuousWorkoutCheck;
                }
                if (timeAssignmentDef == TimeAssignmentDefOf.Sleep)
                {
                    return false;
                }
            }
            return false;
        }
    }
}
