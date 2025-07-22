using RimWorld;
using Verse;
using Verse.AI;

namespace Maux36.Rimbody
{
    public class JobGiver_RecoverWander : JobGiver_Wander
    {
        public override float GetPriority(Pawn pawn)
        {
            return 2.5f;
        }
        public JobGiver_RecoverWander()
        {
            wanderRadius = 7f;
            ticksBetweenWandersRange = new IntRange(RimbodySettings.RecoveryTick, 2 * RimbodySettings.RecoveryTick);
            locomotionUrgency = LocomotionUrgency.Amble;
            wanderDestValidator = (Pawn pawn, IntVec3 loc, IntVec3 root) => WanderRoomUtility.IsValidWanderDest(pawn, loc, root);
        }
        protected override Job TryGiveJob(Pawn pawn)
        {
            pawn.mindState.nextMoveOrderIsWait = false;
            bool flag = pawn.CurJob != null && pawn.CurJob.def == DefOf_Rimbody.Rimbody_RecoverWander;
            bool flying = pawn.Flying;
            bool num = pawn.mindState.nextMoveOrderIsWait && !flying;
            if (!flag)
            {
                pawn.mindState.nextMoveOrderIsWait = !pawn.mindState.nextMoveOrderIsWait;
            }

            if (num && !flag)
            {
                return GetWaitJob();
            }

            IntVec3 exactWanderDest = GetExactWanderDest(pawn);
            if (exactWanderDest == pawn.Position && !flag)
            {
                return GetWaitJob();
            }

            if (!exactWanderDest.IsValid)
            {
                pawn.mindState.nextMoveOrderIsWait = false;
                return null;
            }

            LocomotionUrgency value = locomotionUrgency;
            if (locomotionUrgencyOutsideRadius.HasValue && !pawn.Position.InHorDistOf(GetWanderRoot(pawn), wanderRadius))
            {
                value = locomotionUrgencyOutsideRadius.Value;
            }

            Job job = JobMaker.MakeJob(DefOf_Rimbody.Rimbody_RecoverWander, exactWanderDest);
            job.locomotionUrgency = value;
            job.expiryInterval = expiryInterval;
            job.checkOverrideOnExpire = true;
            job.reportStringOverride = reportStringOverride;
            job.canBashDoors = canBashDoors;
            if (expireOnNearbyEnemy)
            {
                job.expiryInterval = 30;
                job.checkOverrideOnExpire = true;
            }

            DecorateGotoJob(job);
            return job;
            Job GetWaitJob()
            {
                Job job2 = JobMaker.MakeJob(DefOf_Rimbody.Rimbody_RecoverWait);
                job2.expiryInterval = ticksBetweenWandersRange.RandomInRange;
                job2.reportStringOverride = reportStringOverride;
                if (expireOnNearbyEnemy)
                {
                    job2.expiryInterval = 30;
                    job2.checkOverrideOnExpire = true;
                }

                return job2;
            }
        }

        protected override IntVec3 GetWanderRoot(Pawn pawn)
        {
            return pawn.Position;
        }
    }
}