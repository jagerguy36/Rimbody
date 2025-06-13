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
            return base.TryGiveJob(pawn);
        }

        protected override IntVec3 GetWanderRoot(Pawn pawn)
        {
            return pawn.Position;
        }
    }
}