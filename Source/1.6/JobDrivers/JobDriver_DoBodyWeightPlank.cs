using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using UnityEngine;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoBodyWeightPlank : JobDriver
    {
        private const int duration = 1000;
        private float joygainfactor = 1.0f;
        private int tickProgress = 0;
        private float memoryFactor = 1.0f;
        private Vector3 pawnNudge = Vector3.zero;
        private Rot4 lyingRotation = Rot4.Invalid;
        public override Vector3 ForcedBodyOffset
        {
            get
            {
                return pawnNudge;
            }
        }
        public override Rot4 ForcedLayingRotation
        {
            get
            {
                return lyingRotation;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }
            if (job.targetB != null)
            {
                if(!pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed))
                {
                    return false;
                }
            }
            return true;
        }
        private void AddMemory(CompPhysique compPhysique)
        {
            if (compPhysique != null)
            {
                compPhysique.lastWorkoutTick = Find.TickManager.TicksGame;
                compPhysique.AddNewMemory($"balance|{job.def.defName}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref tickProgress, "plank_tickProgress", 0);
            Scribe_Values.Look(ref memoryFactor, "plank_memoryFactor", 1.0f);
            Scribe_Values.Look(ref pawnNudge, "plank_pawnNudget", Vector3.zero);
            Scribe_Values.Look(ref lyingRotation, "plank_lyingRotation", Rot4.Invalid);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var compPhysique = pawn.compPhysique();
            this.AddEndCondition(() => (RimbodySettings.useExhaustion && compPhysique.resting) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            EndOnTired(this);

            //Set up workout
            RimbodyDefLists.BalanceNonTargetJob.TryGetValue(job.def, out var exWorkout);
            float workoutEfficiencyValue = 1f;
            yield return Toils_Reserve.ReserveDestination(TargetIndex.A);
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            Toil workout;
            workout = ToilMaker.MakeToil("MakeNewToils");
            workout.initAction = () =>
            {
                memoryFactor = compPhysique.memory.Contains("balance|" + job.def.defName) ? 0.9f : 1f;
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
                var joyneed = pawn.needs?.joy;
                if (joyneed?.tolerances.BoredOf(DefOf_Rimbody.Rimbody_WorkoutJoy) == true)
                {
                    joygainfactor = 0;
                }
                compPhysique.jobOverride = true;
                compPhysique.strengthOverride = exWorkout.strength * workoutEfficiencyValue;
                compPhysique.cardioOverride = exWorkout.cardio * workoutEfficiencyValue;
                compPhysique.memoryFactorOverride = memoryFactor;
                compPhysique.partsOverride = exWorkout.strengthParts;
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
                compPhysique.jobOverride = false;
                compPhysique.strengthOverride = 0f;
                compPhysique.cardioOverride = 0f;
                compPhysique.memoryFactorOverride = 1f;
                compPhysique.partsOverride = null;
                TryGainGymThought();
                AddMemory(compPhysique);
                pawn.jobs.posture = PawnPosture.Standing;
                pawn.SetPawnBodyAngleOverride(-1f);
            });
            yield return workout;
        }

        public static IJobEndable EndOnTired(IJobEndable f, JobCondition endCondition = JobCondition.InterruptForced)
        {
            Pawn actor = f.GetActor();
            bool isTired = TooTired(actor);
            f.AddEndCondition(() => (!isTired) ? JobCondition.Ongoing : endCondition);
            return f;
        }
        public static bool TooTired(Pawn actor)
        {
            if (((actor != null) & (actor.needs != null)) && actor.needs.rest != null && (double)actor.needs.rest.CurLevel < 0.17f)
            {
                return true;
            }
            return false;
        }
        private void TryGainGymThought()
        {
            var room = pawn.GetRoom();
            if (room == null || room.Role != DefOf_Rimbody.Rimbody_Gym)
            {
                return;
            }

            //get the impressive stage index for the current room
            var scoreStageIndex =
                RoomStatDefOf.Impressiveness.GetScoreStageIndex(room.GetStat(RoomStatDefOf.Impressiveness));
            //if the stage index exists in the definition (in xml), gain the memory (and buff)
            if (DefOf_Rimbody.WorkedOutInImpressiveGym.stages[scoreStageIndex] != null)
            {
                pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(
                    ThoughtMaker.MakeThought(DefOf_Rimbody.WorkedOutInImpressiveGym,
                        scoreStageIndex));
            }
        }
    }
}
