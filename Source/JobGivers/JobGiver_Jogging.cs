using RimWorld;
using Verse.AI;
using Verse;

namespace Maux36.Rimbody
{
    internal class JobGiver_Jogging : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            if (Find.TickManager.TicksGame - compPhysique.lastWorkoutTick < RimbodySettings.RecoveryTick)
            {
                return 0f;
            }
            if (!JoyUtility.EnjoyableOutsideNow(pawn))
            {
                return 0f;
            }

            float result = 5f;

            if (compPhysique.useFatgoal && compPhysique.FatGoal < compPhysique.BodyFat)
            {
                result += 2f + ((compPhysique.BodyFat - compPhysique.FatGoal) / 100f);
            }

            return result;
        }
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Downed || pawn.Drafted)
            {
                return null;
            }
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return null;
            }
            if (!JobDriver_Jogging.TryFindNatureJoggingTarget(pawn, out var interestTarget))
            {
                return null;
            }

            Job job = JobMaker.MakeJob(DefOf_Rimbody.Rimbody_Jogging, interestTarget);
            job.locomotionUrgency = LocomotionUrgency.Sprint;
            return job;
        }
    }
}
