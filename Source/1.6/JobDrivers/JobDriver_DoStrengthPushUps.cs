using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using UnityEngine;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoStrengthPushUps : JobDriver_RimbodyBaseDriver
    {
        private const int duration = 800;
        private float animBase = 0f;
        private float animCoef = 0f;
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
                if (!pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed)) return false;
            }
            return true;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref joygainfactor, "pushup_joygainfactor", 1.0f);
            Scribe_Values.Look(ref tickProgress, "pushup_tickProgress", 0);
            Scribe_Values.Look(ref memoryFactor, "pushup_memoryFactor", 1.0f);
            Scribe_Values.Look(ref workoutEfficiencyValue, "pushup_workoutEfficiencyValue", 1.0f);
            Scribe_Values.Look(ref animBase, "pushup_animBase", 0f);
            Scribe_Values.Look(ref animCoef, "pushup_animCoef", 0f);
            Scribe_Values.Look(ref pawnNudge, "pushup_pawnNudget", Vector3.zero);
            Scribe_Values.Look(ref lyingRotation, "pushup_lyingRotation", Rot4.Invalid);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var compPhysique = pawn.compPhysique();
            this.AddEndCondition(() => (RimbodySettings.useExhaustion && compPhysique.resting) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            this.AddEndCondition(() => (compPhysique.gain >= compPhysique.gainMax) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            Rimbody_Utility.EndOnTired(this);

            //Set up workout
            RimbodyDefLists.JobModExDB.TryGetValue(job.def.shortHash, out var exWorkout);
            memoryFactor = compPhysique.memory.Contains("strength|" + job.def.defName) ? 0.9f : 1f;
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
                animBase = facing.Opposite.AsAngle;
                animCoef = (facing.Opposite.AsAngle > 0 && facing.Opposite.AsAngle < 180) ? -15f : (facing.Opposite.AsAngle > 180 && facing.Opposite.AsAngle < 360) ? 15f : 0f;
                AdjustJoygainFactor();
                StartWorkoutJob(compPhysique, exWorkout);
            };
            float uptime = 0.75f - (20f * compPhysique.MuscleMass / 5000f);
            float cycleDuration = 125f - compPhysique.MuscleMass;
            float jitter_amount = 0.03f * Mathf.Max(0f, (1f - (compPhysique.MuscleMass / 35f)));
            float nudgeMultiplier;
            workout.tickAction = delegate
            {
                tickProgress++;
                float cycleTime = (tickProgress % (int)cycleDuration) / cycleDuration;
                if (cycleTime < uptime)
                {
                    nudgeMultiplier = Mathf.Lerp(0f, 1f, cycleTime / uptime);
                }
                else
                {
                    nudgeMultiplier = Mathf.Lerp(1f, 0f, (cycleTime - uptime) / (1f - uptime));
                }
                pawn.SetPawnBodyAngleOverride(animBase + animCoef * (1f + nudgeMultiplier));
                Vector3 JitterVector = IntVec3.West.RotatedBy(pawn.Rotation).ToVector3() * Rand.RangeSeeded(-jitter_amount, jitter_amount, tickProgress);
                pawnNudge = JitterVector + Vector3.forward*nudgeMultiplier*0.15f;
                pawn.needs?.joy?.GainJoy(joygainfactor * 0.36f / 2500f, DefOf_Rimbody.Rimbody_WorkoutJoy);
            };
            workout.handlingFacing = true;
            workout.defaultCompleteMode = ToilCompleteMode.Delay;
            workout.defaultDuration = duration;
            workout.AddFinishAction(delegate
            {
                FinishWorkout(compPhysique);
                Rimbody_Utility.TryGainGymThought(pawn);
                Rimbody_Utility.AddMemory(compPhysique, RimbodyWorkoutCategory.Strength, job.def.defName);
                pawn.jobs.posture = PawnPosture.Standing;
                pawn.SetPawnBodyAngleOverride(-1f);
            });
            yield return workout;
        }
    }
}
