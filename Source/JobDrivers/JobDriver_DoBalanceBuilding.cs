using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using Verse.Sound;
using UnityEngine;
using System;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoBalanceBuilding : JobDriver
    {
        private const int duration = 1500;
        private float joygainfactor = 1.0f;
        private int tickProgress = 0;
        private Vector3 pawnOffset = Vector3.zero;
        private Vector3 pawnNudge = Vector3.zero;
        private Rot4 lyingRotation = Rot4.Invalid;
        private Building_WorkoutAnimated buildingAnimated => (Building_WorkoutAnimated)job.GetTarget(TargetIndex.A).Thing;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, 0, null, errorOnFailed))
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
                return pawnOffset + pawnNudge;
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
                case Direction.center:
                    pawn.rotationTracker.FaceCell(building.Position);
                    break;
                case Direction.away:
                    pawn.rotationTracker.FaceCell(2 * pawn.Position - building.Position);
                    break;
                case Direction.faceSame:
                    pawn.Rotation = building.Rotation;
                    break;
                case Direction.faceOpposite:
                    pawn.Rotation = building.Rotation.Opposite;
                    break;
                case Direction.lyingFrontSame:
                    pawn.PawnBodyAngleOverride() = building.Rotation.Opposite.AsAngle;
                    pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
                    break;
                case Direction.lyingFrontOpposite:
                    pawn.PawnBodyAngleOverride() = building.Rotation.AsAngle;
                    pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
                    break;
                case Direction.lyingDownSame:
                    pawn.PawnBodyAngleOverride() = building.Rotation.Opposite.AsAngle;
                    pawn.jobs.posture = PawnPosture.LayingOnGroundNormal;
                    lyingRotation = building.Rotation.Opposite == Rot4.South ? Rot4.North : building.Rotation.Opposite;
                    break;
                case Direction.lyingUpSame:
                    pawn.PawnBodyAngleOverride() = building.Rotation.Opposite.AsAngle;
                    pawn.jobs.posture = PawnPosture.LayingOnGroundNormal;
                    lyingRotation = building.Rotation == Rot4.North ? Rot4.South : building.Rotation;
                    break;
            }
        }
        protected void WatchTickAction(Thing building, WorkOut wo, float actorMuscle)
        {
            tickProgress++;
            if (wo.animationType == InteractionType.animation)
            {
                if (tickProgress > 0)
                {
                    if (wo?.pawnAnimPeak?.FromRot(pawn.Rotation) != null && wo?.pawnAnimPeak?.FromRot(pawn.Rotation) != Vector3.zero)
                    {
                        float uptime = 0.75f - (20f * actorMuscle / 5000f);
                        float cycleDuration = 125f - actorMuscle;
                        //float jitter_amount = 3f * Mathf.Max(0f, (1f - (actorMuscle / 35f))) / 100f;
                        float cycleTime = (tickProgress % (int)cycleDuration) / cycleDuration;
                        float nudgeMultiplier;
                        if (cycleTime < uptime)
                        {
                            nudgeMultiplier = Mathf.Lerp(0f, 1f, cycleTime / uptime);
                        }
                        else
                        {
                            nudgeMultiplier = Mathf.Lerp(1f, 0f, (cycleTime - uptime) / (1f - uptime));
                        }

                        //float xJitter = (Rand.RangeSeeded(-jitter_amount, jitter_amount, tickProgress));
                        //Vector3 JitterVector = IntVec3.West.RotatedBy(pawn.Rotation).ToVector3() * xJitter;
                        if (tickProgress > 0)
                        {
                            pawnNudge = nudgeMultiplier * wo.pawnAnimPeak.FromRot(pawn.Rotation);//JitterVector
                        }
                    }
                }
            }
            else if (pawn.IsHashIntervalTick(50 + Rand.Range(0, 10)))
            {
                if (wo.playSound)
                {
                    RimWorld.SoundDefOf.MetalHitImportant.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
                }
                pawn.Drawer.Notify_MeleeAttackOn(building);
            }
            if (joygainfactor > 0)
            {
                pawn.needs?.joy?.GainJoy(1.0f * joygainfactor * 0.36f / 2500f, DefOf_Rimbody.Rimbody_WorkoutJoy);
            }
        }
        private int GetWorkoutInt(CompPhysique compPhysique, ModExtensionRimbodyTarget ext, out float score)
        {
            score = 0f;
            int indexBest = -1;
            var numVarieties = ext.workouts.Count;
            for (int i = 0; i < numVarieties; i++)
            {
                var tempscore = Math.Max(score, compPhysique.GetScore(RimbodyTargetCategory.Balance, ext.workouts[i]));
                if (score < tempscore)
                {
                    score = tempscore;
                    indexBest = i;
                }
            }
            return indexBest;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref tickProgress, "balancebuilding_tickProgress", 0);
            Scribe_Values.Look(ref pawnOffset, "balancebuilding_pawnOffset", Vector3.zero);
            Scribe_Values.Look(ref pawnNudge, "balancebuilding_pawnNudget", Vector3.zero);
            Scribe_Values.Look(ref lyingRotation, "balancebuilding_lyingRotation", Rot4.Invalid);
        }

        private void AddMemory(CompPhysique compPhysique, string name)
        {
            if (compPhysique != null)
            {
                compPhysique.lastWorkoutTick = Find.TickManager.TicksGame;
                compPhysique.AddNewMemory($"balance|{name}");
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            this.EndOnDespawnedOrNull(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.AddEndCondition(() => (RimbodySettings.useExhaustion && compPhysique.resting) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            EndOnTired(this);
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Reserve.Reserve(TargetIndex.B);
            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);

            RimbodyDefLists.BalanceTarget.TryGetValue(TargetThingA.def, out var ext);
            var workoutIndex = GetWorkoutInt(compPhysique, ext, out var score);
            var exWorkout = ext.workouts[workoutIndex];
            if(exWorkout.reportString != null)
            {
                this.job.reportStringOverride = exWorkout.reportString.Translate();
            }
            Toil workout;
            workout = ToilMaker.MakeToil("MakeNewToils");
            workout.initAction = () =>
            {
                GetInPosition(TargetThingA, exWorkout.pawnDirection);
                joygainfactor = TargetThingA.def.GetStatValueAbstract(StatDefOf.JoyGainFactor);
                var joyneed = pawn.needs?.joy;
                if (joyneed?.tolerances.BoredOf(DefOf_Rimbody.Rimbody_WorkoutJoy) == true)
                {
                    joygainfactor = 0;
                }
                compPhysique.jobOverride = true;
                compPhysique.strengthOverride = exWorkout.strength;
                compPhysique.cardioOverride = exWorkout.cardio;
                compPhysique.durationOverride = duration;
                compPhysique.partsOverride = exWorkout.strengthParts;
                if (exWorkout.animationType == InteractionType.animation)
                {
                    if (ext.rimbodyBuildingpartGraphics != null)
                    {
                        buildingAnimated.workoutStartTick = Find.TickManager.TicksGame;
                        buildingAnimated.currentWorkoutIndex = workoutIndex;
                        buildingAnimated.actorMuscle = compPhysique.MuscleMass;
                        buildingAnimated.useJitter = false;
                    }
                    pawnOffset = exWorkout.pawnAnimOffset.FromRot(buildingAnimated.Rotation);
                }
            };
            workout.AddPreTickAction(delegate
            {
                WatchTickAction(TargetThingA, exWorkout, compPhysique.MuscleMass);
            });
            workout.handlingFacing = true;
            workout.defaultCompleteMode = ToilCompleteMode.Delay;
            workout.defaultDuration = duration;
            workout.AddFinishAction(delegate
            {
                compPhysique.jobOverride = false;
                compPhysique.strengthOverride = 0f;
                compPhysique.cardioOverride = 0f;
                compPhysique.durationOverride = 0;
                compPhysique.partsOverride = null;
                if (ext.rimbodyBuildingpartGraphics != null)
                {
                    buildingAnimated.workoutStartTick = -1;
                    buildingAnimated.currentWorkoutIndex = -1;
                    buildingAnimated.actorMuscle = 25;
                    buildingAnimated.useJitter = true;
                }
                pawnOffset = Vector3.zero;
                lyingRotation = Rot4.Invalid;
                TryGainGymThought();
                AddMemory(compPhysique, ext.workouts[workoutIndex].name);
                pawn.PawnBodyAngleOverride() = -1;
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
            if (room == null)
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
