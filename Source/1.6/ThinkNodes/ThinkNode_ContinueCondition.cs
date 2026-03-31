using Verse.AI;
using Verse;
using RimWorld;
using System;

namespace Maux36.Rimbody
{
    internal class ThinkNode_ContinueCondition : ThinkNode_Conditional //Continue Joy workout for assigned tick
    {
        protected override bool Satisfied(Pawn pawn)
        {
            // if (pawn.Downed || pawn.Drafted) // Drafted Condition already handled by ThinkTree
            //     return false;
            var compPhysique = pawn.compPhysique();
            if (compPhysique == null)
                return false;
            // if (RimbodySettings.useExhaustion && compPhysique.resting) // Exhaustion not implemented yet
            //     return false;
            // if (pawn.ageTracker?.CurLifeStage?.developmentalStage != DevelopmentalStage.Adult) // Already checked by whatever prompted Continue condition
            //     return false;
            
            if (compPhysique.AssignedTick <= 0)
                return false;
            if (!Rimbody_Utility.TooTired(pawn))
            {
                TimeAssignmentDef timeAssignmentDef = ((pawn.timetable == null) ? TimeAssignmentDefOf.Anything : pawn.timetable.CurrentAssignment);
                //During Idle time, or little past bed time
                if (timeAssignmentDef == TimeAssignmentDefOf.Anything || timeAssignmentDef == TimeAssignmentDefOf.Joy || timeAssignmentDef == TimeAssignmentDefOf.Sleep)
                {
                    //You are still doing the "Set"
                    if (Find.TickManager.TicksGame - compPhysique.lastWorkoutTick < RimbodySettings.RecoveryTick * 4f)
                    {
                        if (!timeAssignmentDef.allowJoy || pawn.needs.joy.tolerances.BoredOf(DefOf_Rimbody.Rimbody_WorkoutJoy) || HealthAIUtility.ShouldSeekMedicalRest(pawn))
                        {
                            compPhysique.AssignedTick = 0;
                            return false;
                        }
                        return true;
                    }
                }
            }
            compPhysique.AssignedTick = 0;
            return false;
        }
    }
}
