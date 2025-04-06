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
            if (pawn != null && pawn.ageTracker?.CurLifeStage?.developmentalStage == DevelopmentalStage.Adult && !HealthAIUtility.ShouldSeekMedicalRest(pawn))
            {
                if (pawn.needs?.rest?.CurLevel < 0.17f) //Too tired
                {
                    return 0f;
                }
                if (pawn.timetable?.CurrentAssignment == DefOf_Rimbody.Rimbody_Workout) //Workout schedule
                {
                    var compPhysique = pawn.TryGetComp<CompPhysique>();
                    if (compPhysique != null)
                    {
                        if (RimbodySettings.useFatigue && compPhysique.resting)
                        {
                            return 0f;
                        }
                        return 9f;
                    }

                    return 0f;
                }
            }
            return 0f;
        }
    }
}
