using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoStrengthBuilding : JobDriver
    {
        private const int duration = 1000;
        private float joygainfactor = 1.0f;
        private int tickProgress = 0;
        private int workoutIndex = -1;
        private float memoryFactor = 1.0f;
        private float workoutEfficiencyValue = 1.0f;
        private Vector3 pawnOffset = Vector3.zero;
        private Rot4 lyingRotation = Rot4.Invalid;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }
            if (!pawn.ReserveSittableOrSpot(job.targetB.Cell, job, errorOnFailed))
            {
                return false;
            }
            return true;
        }
        public override Vector3 ForcedBodyOffset
        {
            get
            {
                return pawnOffset;
            }
        }
        public override Rot4 ForcedLayingRotation
        {
            get
            {
                return lyingRotation;
            }
        }

        protected void GetInPosition(Thing building, Direction direction)
        {
            switch (direction)
            {
                case Direction.Center:
                    pawn.rotationTracker.FaceCell(building.Position);
                    break;
                case Direction.Away:
                    pawn.rotationTracker.FaceCell(2 * pawn.Position - building.Position);
                    break;
                case Direction.FaceSame:
                    pawn.Rotation = building.Rotation;
                    break;
                case Direction.FaceOpposite:
                    pawn.Rotation = building.Rotation.Opposite;
                    break;
                case Direction.LyingFrontSame:
                    pawn.PawnBodyAngleOverride() = building.Rotation.Opposite.AsAngle;
                    pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
                    break;
                case Direction.LyingFrontOpposite:
                    pawn.PawnBodyAngleOverride() = building.Rotation.AsAngle;
                    pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
                    break;
                case Direction.LyingDownSame:
                    pawn.PawnBodyAngleOverride() = building.Rotation.Opposite.AsAngle;
                    pawn.jobs.posture = PawnPosture.LayingOnGroundNormal;
                    lyingRotation = building.Rotation.Opposite == Rot4.South ? Rot4.North : building.Rotation.Opposite;
                    break;
                case Direction.LyingUpSame:
                    pawn.PawnBodyAngleOverride() = building.Rotation.Opposite.AsAngle;
                    pawn.jobs.posture = PawnPosture.LayingOnGroundNormal;
                    lyingRotation = building.Rotation == Rot4.North ? Rot4.South : building.Rotation;
                    break;
            }
        }
        protected void WatchTickAction(Building_WorkoutAnimated building, WorkOut wo, float uptime, float cycleDuration, float jitter_amount)
        {
            tickProgress++;
            if (wo.animationType == InteractionType.Building)
            {
                float cycleTime = (tickProgress % (int)cycleDuration) / cycleDuration;
                float nudgeMultiplier;
                Vector3 buildingOffset = Vector3.zero;
                if (cycleTime < uptime) nudgeMultiplier = Mathf.Lerp(0f, 1f, cycleTime / uptime);
                else nudgeMultiplier = Mathf.Lerp(1f, 0f, (cycleTime - uptime) / (1f - uptime));
                //Pawn
                if (wo?.pawnAnimOffset?.FromRot(building.Rotation) != null)
                {
                    pawnOffset = wo.pawnAnimOffset.FromRot(building.Rotation);
                }
                if (wo?.pawnAnimPeak?.FromRot(pawn.Rotation) != null && wo?.pawnAnimPeak?.FromRot(pawn.Rotation) != Vector3.zero)
                {
                    pawnOffset += nudgeMultiplier * wo.pawnAnimPeak.FromRot(pawn.Rotation) + IntVec3.West.RotatedBy(pawn.Rotation).ToVector3() * Rand.Range(-jitter_amount, jitter_amount);
                }
                //Building
                if (wo?.movingpartAnimOffset?.FromRot(building.Rotation) != null)
                {
                    buildingOffset = wo.movingpartAnimOffset.FromRot(building.Rotation);
                }
                if (wo?.movingpartAnimPeak?.FromRot(building.Rotation) != null)
                {
                    buildingOffset += nudgeMultiplier * wo.movingpartAnimPeak.FromRot(building.Rotation) + IntVec3.West.RotatedBy(building.Rotation).ToVector3() * Rand.Range(-jitter_amount, jitter_amount);
                }
                building.calculatedOffset = buildingOffset;
            }
            else if (wo.animationType == InteractionType.Melee)
            {
                if (pawn.IsHashIntervalTick(50 + Rand.Range(0, 10)))
                {
                    if (wo.playSound)
                    {
                        SoundDefOf.MetalHitImportant.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
                    }
                    if(building != null)
                    {
                        pawn.Drawer.Notify_MeleeAttackOn(building);
                    }
                }
            }

            if (joygainfactor > 0)
            {
                pawn.needs?.joy?.GainJoy(1.0f * joygainfactor * 0.36f / 2500f, DefOf_Rimbody.Rimbody_WorkoutJoy);
            }
        }
        private int GetWorkoutInt(CompPhysique compPhysique, ModExtensionRimbodyTarget ext, out float memoryFactor)
        {
            float score = 0f;
            memoryFactor = 1f;
            int indexBest = -1;
            var numVarieties = ext.workouts.Count;
            if (numVarieties == 1)
            {
                memoryFactor = compPhysique.memory.Contains("strength|" + ext.workouts[0].name) ? 0.9f : 1f;
                return 0;
            }
            for (int i = 0; i < numVarieties; i++)
            {
                if (ext.workouts[i].Category != RimbodyWorkoutCategory.Strength)
                {
                    continue;
                }
                float tmpMemoryFactor = compPhysique.memory.Contains("strength|" + ext.workouts[i].name) ? 0.9f : 1f;
                float tmpScore = tmpMemoryFactor * compPhysique.GetWorkoutScore(RimbodyWorkoutCategory.Strength, ext.workouts[i]);
                if (tmpScore > score)
                {
                    score = tmpScore;
                    memoryFactor = tmpMemoryFactor;
                    indexBest = i;
                }
                else if (tmpScore == score)
                {
                    if (Rand.Chance(0.5f))
                    {
                        score = tmpScore;
                        memoryFactor = tmpMemoryFactor;
                        indexBest = i;
                    }
                }
            }
            return indexBest;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref joygainfactor, "strengthbuilding_joygainfactor", 1.0f);
            Scribe_Values.Look(ref tickProgress, "strengthbuilding_tickProgress", 0);
            Scribe_Values.Look(ref workoutIndex, "strengthbuilding_workoutIndex", -1);
            Scribe_Values.Look(ref memoryFactor, "strengthbuilding_memoryFactor", 1f);
            Scribe_Values.Look(ref workoutEfficiencyValue, "strengthbuilding_workoutEfficiencyValue", 1f);
            Scribe_Values.Look(ref pawnOffset, "strengthbuilding_pawnOffset", Vector3.zero);
            Scribe_Values.Look(ref lyingRotation, "strengthbuilding_lyingRotation", Rot4.Invalid);
        }

        private void AddMemory(CompPhysique compPhysique, string name)
        {
            if (compPhysique != null)
            {
                compPhysique.lastWorkoutTick = Find.TickManager.TicksGame;
                compPhysique.AddNewMemory($"strength|{name}");
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var compPhysique = pawn.compPhysique();
            this.EndOnDespawnedOrNull(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.AddEndCondition(() => (RimbodySettings.useExhaustion && compPhysique.resting) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            this.AddEndCondition(() => (compPhysique.gain >= compPhysique.gainMax) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            EndOnTired(this);
            RimbodyDefLists.StrengthTarget.TryGetValue(TargetThingA.def, out var ext);
            Building_WorkoutAnimated buildingAnimated = job.GetTarget(TargetIndex.A).Thing as Building_WorkoutAnimated;

            if (workoutIndex < 0) workoutIndex = GetWorkoutInt(compPhysique, ext, out memoryFactor);
            var exWorkout = ext.workouts[workoutIndex];
            workoutEfficiencyValue = TargetThingA.GetStatValue(DefOf_Rimbody.Rimbody_WorkoutEfficiency);

            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);

            if (exWorkout.reportString != null)
            {
                this.job.reportStringOverride = exWorkout.reportString.Translate();
            }
            Toil workout;
            workout = ToilMaker.MakeToil("MakeNewToils");
            workout.initAction = () =>
            {
                TargetThingA.Map.physicalInteractionReservationManager.Reserve(pawn, job, TargetThingA);
                GetInPosition(TargetThingA, exWorkout.pawnDirection);
                joygainfactor = TargetThingA.def.GetStatValueAbstract(StatDefOf.JoyGainFactor);
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
                if (buildingAnimated != null)
                {
                    buildingAnimated.beingUsed = true;
                }
            };
            float uptime = 0.95f - (0.0004f * compPhysique.MuscleMass);
            float cycleDuration = 125f - compPhysique.MuscleMass;
            float jitter_amount = 0.03f * Mathf.Max(0f, (1f - (compPhysique.MuscleMass / 35f)));
            workout.AddPreTickAction(delegate
            {
                WatchTickAction(buildingAnimated, exWorkout, uptime, cycleDuration, jitter_amount);
            });
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
                if (buildingAnimated != null)
                {
                    buildingAnimated.beingUsed = false;
                    buildingAnimated.calculatedOffset = Vector3.zero;
                    pawnOffset = Vector3.zero;
                }
                pawn.PawnBodyAngleOverride() = -1;
                lyingRotation = Rot4.Invalid;
                TryGainGymThought();
                AddMemory(compPhysique, ext.workouts[workoutIndex].name);
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
