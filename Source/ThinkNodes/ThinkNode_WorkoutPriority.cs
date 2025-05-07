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
                //Workout schedule
                if (timeAssignmentDef == DefOf_Rimbody.Rimbody_Workout)
                {
                    if (pawn.needs?.rest?.CurLevel < 0.17f || HealthAIUtility.ShouldSeekMedicalRest(pawn)) //Should rest
                    {
                        return 0f;
                    }
                    var compPhysique = pawn.TryGetComp<CompPhysique>();
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
                if (JoyUtility.LordPreventsGettingJoy(pawn))
                {
                    return 0f;
                }
                float curLevel = pawn.needs.joy.CurLevel;
                if (!timeAssignmentDef.allowJoy)
                {
                    return 0f;
                }
                if (timeAssignmentDef == TimeAssignmentDefOf.Anything)
                {
                    if (curLevel < 0.35f)
                    {
                        return 6f;
                    }
                    return 0f;
                }
                if (timeAssignmentDef == TimeAssignmentDefOf.Joy)
                {
                    if (curLevel < 0.95f)
                    {
                        return 7f;
                    }
                    return 0f;
                }
                if (timeAssignmentDef == TimeAssignmentDefOf.Sleep)
                {
                    if (curLevel < 0.95f)
                    {
                        return 2f;
                    }
                    return 0f;
                }
            }
            return 0f;
        }
    }
}
