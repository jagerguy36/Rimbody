using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using Verse.Sound;
using System.Reflection;
using System;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoBalanceBuilding : JobDriver
    {
        private float joygainfactor = 1.0f;

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
                case Direction.rotSame:
                    pawn.PawnBodyAngleOverride() = building.Rotation.Opposite.AsAngle;
                    pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
                    break;
                case Direction.rotOpposite:
                    pawn.PawnBodyAngleOverride() = building.Rotation.AsAngle;
                    pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
                    break;
                case Direction.rotClock:
                    pawn.PawnBodyAngleOverride() = building.Rotation.Opposite.AsAngle + 90f;
                    pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
                    break;
                case Direction.rotAntiClock:
                    pawn.PawnBodyAngleOverride() = building.Rotation.Opposite.AsAngle + 270f % 360f;
                    pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
                    break;
            }
        }
        protected void WatchTickAction(Thing building, bool interact, bool playSound)
        {
            if (interact && pawn.IsHashIntervalTick(50 + Rand.Range(0, 10)))
            {
                if (playSound)
                {
                    RimWorld.SoundDefOf.MetalHitImportant.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
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

            var ext = TargetThingA.def.GetModExtension<ModExtensionRimbodyTarget>();
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
                compPhysique.limitOverride = score <= 0.9f;
                compPhysique.strengthOverride = exWorkout.strength * score;
                compPhysique.cardioOverride = exWorkout.cardio * score;
            };
            workout.AddPreTickAction(delegate
            {
                WatchTickAction(TargetThingA, ext.rimbodyBuildingpartGraphics == null, exWorkout.playSound);
            });
            workout.handlingFacing = true;
            workout.defaultCompleteMode = ToilCompleteMode.Delay;
            workout.defaultDuration = 1500;
            workout.AddFinishAction(delegate
            {
                compPhysique.jobOverride = false;
                compPhysique.limitOverride = false;
                compPhysique.strengthOverride = 0f;
                compPhysique.cardioOverride = 0f;
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
