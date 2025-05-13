using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoCardioBuilding : JobDriver
    {
        private const int duration = 2500;
        private float joygainfactor = 1.0f;
        private int workoutIndex = -1;
        private float workoutEfficiencyValue = 1.0f;

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
            job.targetA.Thing.Map.physicalInteractionReservationManager.Reserve(pawn, job, job.targetA.Thing);

            return true;
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
            }
        }
        protected void WatchTickAction()
        {
            if (joygainfactor > 0)
            {
                pawn.needs?.joy?.GainJoy(1.0f * joygainfactor * 0.36f / 2500f, DefOf_Rimbody.Rimbody_WorkoutJoy);
            }
        }
        private int GetWorkoutInt(CompPhysique compPhysique, ModExtensionRimbodyTarget ext)
        {
            float score = 0f;
            int indexBest = -1;
            var numVarieties = ext.workouts.Count;
            if (numVarieties == 1)
            {
                return 0;
            }
            for (int i = 0; i < numVarieties; i++)
            {
                if (ext.workouts[i].Category != RimbodyWorkoutCategory.Cardio)
                {
                    continue;
                }
                float tmpScore = compPhysique.GetWorkoutScore(RimbodyWorkoutCategory.Cardio, ext.workouts[i]);
                if (tmpScore > score)
                {
                    score = tmpScore;
                    indexBest = i;
                }
                else if (tmpScore == score)
                {
                    if (Rand.Chance(0.5f))
                    {
                        score = tmpScore;
                        indexBest = i;
                    }
                }
            }
            return indexBest;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref joygainfactor, "cardiobuilding_joygainfactor", 1.0f);
            Scribe_Values.Look(ref workoutIndex, "cardiobuilding_workoutIndex", -1);
            Scribe_Values.Look(ref workoutEfficiencyValue, "cardiobuilding_workoutEfficiencyValue", 1f);
        }

        private void AddMemory(CompPhysique compPhysique, string name)
        {
            if (compPhysique != null)
            {
                compPhysique.lastWorkoutTick = Find.TickManager.TicksGame;
                compPhysique.AddNewMemory($"cardio|{name}");
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var compPhysique = pawn.compPhysique();
            this.EndOnDespawnedOrNull(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.AddEndCondition(() => (RimbodySettings.useExhaustion && compPhysique.resting) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            EndOnTired(this);
            RimbodyDefLists.CardioTarget.TryGetValue(TargetThingA.def, out var ext);
            Building_WorkoutAnimated buildingAnimated = job.GetTarget(TargetIndex.A).Thing as Building_WorkoutAnimated;

            if (workoutIndex < 0) workoutIndex = GetWorkoutInt(compPhysique, ext);
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
                compPhysique.partsOverride = exWorkout.strengthParts;
            };
            workout.AddPreTickAction(delegate
            {
                WatchTickAction();
            });
            workout.handlingFacing = true;
            workout.defaultCompleteMode = ToilCompleteMode.Delay;
            workout.defaultDuration = duration;
            workout.AddFinishAction(delegate
            {
                compPhysique.jobOverride = false;
                compPhysique.strengthOverride = 0f;
                compPhysique.cardioOverride = 0f;
                compPhysique.partsOverride = null;
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
