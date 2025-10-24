using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using UnityEngine;
using Verse;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoCardioBuilding : JobDriver_RimbodyBaseDriver
    {
        private const int duration = 2500;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed)) return false;
            if (!pawn.ReserveSittableOrSpot(job.targetB.Cell, job, errorOnFailed)) return false;
            return true;
        }
        protected void WatchTickAction()
        {
            tickProgress++;
            if (joygainfactor > 0)
            {
                pawn.needs?.joy?.GainJoy(1.0f * joygainfactor * 0.36f / 2500f, DefOf_Rimbody.Rimbody_WorkoutJoy);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref joygainfactor, "cardiobuilding_joygainfactor", 1.0f);
            Scribe_Values.Look(ref tickProgress, "cardiobuilding_tickProgress", 0);
            Scribe_Values.Look(ref workoutIndex, "cardiobuilding_workoutIndex", -1);
            Scribe_Values.Look(ref workoutEfficiencyValue, "cardiobuilding_workoutEfficiencyValue", 1f);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            var compPhysique = pawn.compPhysique();
            this.EndOnDespawnedOrNull(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.AddEndCondition(() => (RimbodySettings.useExhaustion && compPhysique.resting) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            Rimbody_Utility.EndOnTired(this);
            RimbodyDefLists.ThingModExDB.TryGetValue(TargetThingA.def.shortHash, out var ext);
            Building_WorkoutAnimated buildingAnimated = TargetThingA as Building_WorkoutAnimated;

            if (workoutIndex < 0) workoutIndex = GetWorkoutInt(compPhysique, ext, RimbodyWorkoutCategory.Cardio, out _);
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
                AdjustJoygainFactor();
                StartWorkout(compPhysique, exWorkout);
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
                FinishWorkout(compPhysique);
                Rimbody_Utility.TryGainGymThought(pawn);
                Rimbody_Utility.AddMemory(compPhysique, RimbodyWorkoutCategory.Cardio, ext.workouts[workoutIndex].name);
            });
            yield return workout;
        }
    }
}
