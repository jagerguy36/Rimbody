using Verse.AI;
using Verse;

namespace Maux36.Rimbody
{
    internal class ThinkNode_WorkoutCondition : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn?.timetable?.CurrentAssignment == DefOf_Rimbody.Rimbody_Workout && pawn.ageTracker?.CurLifeStage?.developmentalStage==DevelopmentalStage.Adult)
                return true;
            return false;
        }
    }
}
