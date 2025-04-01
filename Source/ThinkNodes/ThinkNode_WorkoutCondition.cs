using Verse.AI;
using Verse;
using RimWorld;
using System;

namespace Maux36.Rimbody
{
    internal class ThinkNode_WorkoutCondition : ThinkNode_Conditional
    {
        private const int GameStartNoIdleWorkoutTicks = 60000;
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
                    var compPhysique = pawn.TryGetComp<CompPhysique>(); //Goal not satisfied
                    if (compPhysique == null)
                    {
                        return false;
                    }
                    if (compPhysique.resting)
                    {
                        return false;
                    }

                    var need = pawn.needs?.joy;
                    //Idle Joy
                    if (need != null)
                    {
                        if (need.CurLevel < 0.9 && !need.tolerances.BoredOf(DefOf_Rimbody.Rimbody_WorkoutJoy) && Find.TickManager.TicksGame >= 60000)
                        {
                            return true;
                        }
                    }
                    //Meet Goal
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
