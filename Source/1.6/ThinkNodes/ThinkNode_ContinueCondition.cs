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
                if (compPhysique.AssignedTick > 0)
                {
                    if (timeAssignmentDef == TimeAssignmentDefOf.Anything || timeAssignmentDef == TimeAssignmentDefOf.Joy || timeAssignmentDef == TimeAssignmentDefOf.Sleep)
                    {
                        if (Find.TickManager.TicksGame - compPhysique.lastWorkoutTick < RimbodySettings.RecoveryTick * 4f)
                        {
                            if (HealthAIUtility.ShouldSeekMedicalRest(pawn) || !timeAssignmentDef.allowJoy || pawn.needs.joy.tolerances.BoredOf(DefOf_Rimbody.Rimbody_WorkoutJoy))
                            {
                                compPhysique.AssignedTick = 0;
                                return false;
                            }
                            return true;
                        }
                    }
                    compPhysique.AssignedTick = 0;
                    return false;
                }
            }
            return false;
        }
    }
}
