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
            var compPhysique = pawn.compPhysique();
            if (compPhysique == null)
                return 0f;
            // if (RimbodySettings.useExhaustion && compPhysique.resting) // Exhaustion not implemented yet
            //     return 0f;

            if (pawn.ageTracker?.CurLifeStage?.developmentalStage != DevelopmentalStage.Adult)
                return 0f;
            if (Rimbody_Utility.TooTired(pawn))
                return 0f;
            //No need to check Colonist / Prisoner etc condition as this only concerns pawns whose schedule can be controlled by the player.

            //Workout schedule
            TimeAssignmentDef timeAssignmentDef = ((pawn.timetable == null) ? TimeAssignmentDefOf.Anything : pawn.timetable.CurrentAssignment);
            if (timeAssignmentDef == DefOf_Rimbody.Rimbody_Workout)
            {
                if (HealthAIUtility.ShouldSeekMedicalRest(pawn))
                {
                    return 0f;
                }
                return 9f;
            }
            return 0f;
        }
    }
}
