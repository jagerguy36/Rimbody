using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using UnityEngine;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoBodyWeightPlank : JobDriver_RimbodyBaseDriver
    {
        private const int duration = 1000;
        private Vector3 pawnNudge = Vector3.zero;
        public override Vector3 ForcedBodyOffset
        {
            get
            {
                return pawnNudge;
            }
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed)) return false;
            if (job.targetB != null)
            {
                if(!pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed)) return false;
            }
            return true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref joygainfactor, "plank_joygainfactor", 1.0f);
            Scribe_Values.Look(ref tickProgress, "plank_tickProgress", 0);
            Scribe_Values.Look(ref memoryFactor, "plank_memoryFactor", 1.0f);
            Scribe_Values.Look(ref workoutEfficiencyValue, "plank_workoutEfficiencyValue", 1f);
            Scribe_Values.Look(ref pawnNudge, "plank_pawnNudget", Vector3.zero);
            Scribe_Values.Look(ref lyingRotation, "plank_lyingRotation", Rot4.Invalid);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var compPhysique = pawn.compPhysique();
            this.AddEndCondition(() => (RimbodySettings.useExhaustion && compPhysique.resting) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            Rimbody_Utility.EndOnTired(this);

            //Set up workout
            RimbodyDefLists.JobModExDB.TryGetValue(job.def.shortHash, out var exWorkout);
            memoryFactor = compPhysique.memory.Contains("balance|" + job.def.defName) ? 0.9f : 1f;
            yield return Toils_Reserve.ReserveDestination(TargetIndex.A);
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            Toil workout;
            workout = ToilMaker.MakeToil("MakeNewToils");
            workout.initAction = () =>
            {
                pawn.pather.StopDead();
                pawn.jobs.posture = PawnPosture.LayingOnGroundNormal;
                Rot4 facing = facing = Rot4.Random;
                if (TargetThingB != null)
                {
                    facing = TargetThingB.Rotation;
                    workoutEfficiencyValue = 1.05f;
                }
                lyingRotation = facing.Opposite == Rot4.South ? Rot4.North : facing.Opposite;
                pawn.SetPawnBodyAngleOverride(facing.Opposite.AsAngle + ((facing.Opposite.AsAngle > 0 && facing.Opposite.AsAngle < 180) ? -30f : (facing.Opposite.AsAngle > 180 && facing.Opposite.AsAngle < 360) ? 30f : 0f));
                lyingRotation = facing.Opposite == Rot4.South ? Rot4.North : facing.Opposite;
                AdjustJoygainFactor();
                StartWorkoutJob(compPhysique, exWorkout);
            };
            float jitter_amount = 3f * Mathf.Max(0f, (1f - (compPhysique.MuscleMass / 35f))) / 100f;
            workout.tickAction = delegate
            {
                tickProgress += 1;
                pawn.needs?.joy?.GainJoy(1.0f * joygainfactor * 0.36f / 2500f, DefOf_Rimbody.Rimbody_WorkoutJoy);
                float xJitter = (Rand.RangeSeeded(-jitter_amount, jitter_amount, tickProgress));
                Vector3 JitterVector = IntVec3.West.RotatedBy(pawn.Rotation).ToVector3() * xJitter;
                pawnNudge = JitterVector;
                if (tickProgress % 150 == 0)
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, DefOf_Rimbody.Mote_Rimbody_Plank);
                }
            };
            workout.handlingFacing = true;
            workout.defaultCompleteMode = ToilCompleteMode.Delay;
            workout.defaultDuration = duration;
            workout.AddFinishAction(delegate
            {
                FinishWorkout(compPhysique);
                Rimbody_Utility.TryGainGymThought(pawn);
                Rimbody_Utility.AddMemory(compPhysique, RimbodyWorkoutCategory.Balance, job.def.defName);
                pawn.jobs.posture = PawnPosture.Standing;
                pawn.SetPawnBodyAngleOverride(-1f);
            });
            yield return workout;
        }
    }
}
