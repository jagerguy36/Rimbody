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
            if (pawn != null && pawn.ageTracker?.CurLifeStage?.developmentalStage == DevelopmentalStage.Adult)
            {
                if (pawn.IsColonist || pawn.IsPrisonerOfColony)
                {
                    if (pawn.Downed || pawn.Drafted) return false;
                    if (Rimbody_Utility.TooTired(pawn)) return false;
                    var compPhysique = pawn.compPhysique();
                    if (compPhysique == null)
                    {
                        return false;
                    }
                    if (RimbodySettings.useExhaustion && compPhysique.resting)
                    {
                        return false;
                    }
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
                }
            }
            return false;
        }
    }
}
