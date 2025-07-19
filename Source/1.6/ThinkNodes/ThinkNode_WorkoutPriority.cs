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
                TimeAssignmentDef timeAssignmentDef = ((pawn.timetable == null) ? TimeAssignmentDefOf.Anything : pawn.timetable.CurrentAssignment);
                var compPhysique = pawn.compPhysique();
                if (compPhysique == null)
                {
                    return 0;
                }
                //Workout schedule
                if (timeAssignmentDef == DefOf_Rimbody.Rimbody_Workout)
                {
                    if (pawn.needs?.rest?.CurLevel < 0.17f || HealthAIUtility.ShouldSeekMedicalRest(pawn)) //Should rest
                    {
                        return 0f;
                    }
                    if (compPhysique != null)
                    {
                        if (RimbodySettings.useExhaustion && compPhysique.resting)
                        {
                            return 0f;
                        }
                        return 9f;
                    }
                    return 0f;
                }
                //For joy
                if (pawn.needs.joy == null)
                {
                    return 0f;
                }
                if (Find.TickManager.TicksGame < 60000) // No for joy workout for a day
                {
                    return 0f;
                }
                if (JoyUtility.LordPreventsGettingJoy(pawn))
                {
                    return 0f;
                }
                float curLevel = pawn.needs.joy.CurLevel;
                if (!timeAssignmentDef.allowJoy)
                {
                    return 0f;
                }
                if (pawn.needs.joy.tolerances.BoredOf(DefOf_Rimbody.Rimbody_WorkoutJoy))
                {
                    return 0f;
                }
                float continuousWorkoutOffset = 0;
                if (Find.TickManager.TicksGame - compPhysique.lastWorkoutTick < RimbodySettings.RecoveryTick * 4f) continuousWorkoutOffset = 0.1f;
                if (timeAssignmentDef == TimeAssignmentDefOf.Anything)
                {
                    if (curLevel < 0.35f)
                    {
                        return 6f + continuousWorkoutOffset;
                    }
                    return 0f;
                }
                if (timeAssignmentDef == TimeAssignmentDefOf.Joy)
                {
                    if (curLevel < 0.95f)
                    {
                        if (!RimbodySettings.workoutDuringRecTime) return 5f + continuousWorkoutOffset;
                        return 7f + continuousWorkoutOffset;
                    }
                    return 0f;
                }
                if (timeAssignmentDef == TimeAssignmentDefOf.Sleep)
                {
                    if (curLevel < 0.95f)
                    {
                        return 2f + continuousWorkoutOffset;
                    }
                    return 0f;
                }
            }
            return 0f;
        }
    }
}
