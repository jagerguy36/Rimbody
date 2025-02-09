using Verse.AI;
using Verse;
using RimWorld;
using System;

namespace Maux36.Rimbody
{
    internal class ThinkNode_WorkoutCondition : ThinkNode_Conditional
    {
        private const int GameStartNoIdleJoyTicks = 60000;
        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn != null && pawn.ageTracker?.CurLifeStage?.developmentalStage == DevelopmentalStage.Adult && !HealthAIUtility.ShouldSeekMedicalRest(pawn))
            {
                if (pawn.needs?.rest?.CurLevel < 0.17f) //Too tired
                {
                    return false;
                }
                if (pawn.IsColonist || pawn.IsPrisonerOfColony)
                {
                    if (pawn.timetable?.CurrentAssignment == DefOf_Rimbody.Rimbody_Workout) //Workout schedule
                    {
                        return true;
                    }
                    var need = pawn.needs?.joy;
                    if (need != null) //Idle Joy
                    {
                        if (need.CurLevel < 0.9 && !need.tolerances.BoredOf(DefOf_Rimbody.Rimbody_WorkoutJoy) && Find.TickManager.TicksGame >= 60000)
                        {
                            return true;
                        }
                    }
                    var compPhysique = pawn.TryGetComp<CompPhysique>(); //Goal not satisfied
                    if ((compPhysique.useMuscleGoal && compPhysique.MuscleGoal > compPhysique.MuscleMass) || (compPhysique.useFatgoal && compPhysique.FatGoal < compPhysique.BodyFat))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
