using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using Verse;

namespace Maux36.Rimbody
{
    internal class JobGiver_DoChunkLifting : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            if (Find.TickManager.TicksGame - compPhysique.lastWorkoutTick < RimbodySettings.RecoveryTick)
            {
                return 0f;
            }

            float result = 5.0f; //5.5f;

            if (compPhysique.useMuscleGoal && compPhysique.MuscleGoal > compPhysique.MuscleMass)
            {
                result += 2.5f + ((compPhysique.MuscleGoal - compPhysique.MuscleMass)/100f);
            }
            else
            {
                result += (25f - compPhysique.MuscleMass) / 100f;
            }

            if (compPhysique.gain >= ((2f * compPhysique.MuscleMass * compPhysique.MuscleGainFactor) + 100f))
            {
                result -= 4f;
            }
            return result;
        }

        public static bool TooTired(Pawn actor)
        {
            if (((actor != null) & (actor.needs != null)) && actor.needs.rest != null && (double)actor.needs.rest.CurLevel < 0.2f)
            {
                return true;
            }
            return false;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Downed || pawn.Drafted)
            {
                return null;
            }
            if (TooTired(pawn))
            {
                return null;
            }
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return null;
            }
            bool predicate(Thing t)
            {
                if (!pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Some))
                {
                    return false;
                }
                if (t.IsForbidden(pawn))
                {
                    return false;
                }
                //Haul하라고 마킹해있어도 노노노
                return true;
            }

            Thing Chunk = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Chunk),
                PathEndMode.OnCell,
                TraverseParms.For(pawn, Danger.Some),
                9999f,
                predicate
            );


            if (Chunk != null)
            {
                Job job = DoTryGiveJob(pawn, Chunk);
                if (job != null)
                {
                    return job;
                }
            }
            return null;
        }
        public Job DoTryGiveJob(Pawn pawn, Thing t)
        {
            if (pawn.CanReserveAndReach(t, PathEndMode.OnCell, Danger.Some, 1, -1, null, false))
            {
                return JobMaker.MakeJob(DefOf_Rimbody.Rimbody_DoChunkLifting, t);
            }
            return null;
        }
    }
}