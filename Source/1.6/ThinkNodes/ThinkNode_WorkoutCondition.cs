using Verse.AI;
using Verse;
using RimWorld;
using System;

namespace Maux36.Rimbody
{
    internal class ThinkNode_WorkoutCondition : ThinkNode_Conditional //Satisfy goal when Idle
    {
        protected override bool Satisfied(Pawn pawn)
        {
            var compPhysique = pawn.compPhysique();
            if (compPhysique == null)
                return false;
            // if (RimbodySettings.useExhaustion && compPhysique.resting) // Exhaustion not implemented yet
            //     return false;

            if (pawn.ageTracker?.CurLifeStage?.developmentalStage != DevelopmentalStage.Adult)
                return false;
            if (Rimbody_Utility.TooTired(pawn))
                return false;
            if (!pawn.IsColonist && !pawn.IsPrisonerOfColony)
                return false;

            //Meet Goal
            if (compPhysique.useFatgoal && compPhysique.FatGoal < compPhysique.BodyFat)
            {
                if (HealthAIUtility.ShouldSeekMedicalRest(pawn))
                {
                    return false;
                }
                return true;
            }
            if (compPhysique.useMuscleGoal && compPhysique.MuscleGoal > compPhysique.MuscleMass)
            {
                if(compPhysique.gain >= compPhysique.gainMax * RimbodySettings.gainMaxGracePeriod)
                {
                    return false;
                }
                if (HealthAIUtility.ShouldSeekMedicalRest(pawn))
                {
                    return false;
                }
                return true;
            }
            return false;
        }
    }
}
