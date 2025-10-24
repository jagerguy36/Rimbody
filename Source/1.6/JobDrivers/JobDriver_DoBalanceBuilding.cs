using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoBalanceBuilding : JobDriver_RimbodyBaseDriver
    {
        private const int duration = 1500;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed)) return false;
            if (!pawn.ReserveSittableOrSpot(job.targetB.Cell, job, errorOnFailed)) return false;
            return true;
        }
        protected void WatchTickAction(Building_WorkoutAnimated building, WorkOut wo, float uptime, float cycleDuration)
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
                    pawnOffset += nudgeMultiplier * wo.pawnAnimPeak.FromRot(pawn.Rotation);
                }
                //Building
                if (wo?.movingpartAnimOffset?.FromRot(building.Rotation) != null)
                {
                    buildingOffset = wo.movingpartAnimOffset.FromRot(building.Rotation);
                }
                if (wo?.movingpartAnimPeak?.FromRot(building.Rotation) != null)
                {
                    buildingOffset += nudgeMultiplier * wo.movingpartAnimPeak.FromRot(building.Rotation);
                }
                building.calculatedOffset = buildingOffset;
            }
            else if (wo.animationType == InteractionType.Melee)
            {
                if (pawn.IsHashIntervalTick(50 + Rand.Range(0, 10)))
                {
                    if (wo.playSound)
                    {
                        RimWorld.SoundDefOf.MetalHitImportant.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
                    }
                    if (building != null)
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
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref joygainfactor, "balancebuilding_joygainfactor", 1.0f);
            Scribe_Values.Look(ref tickProgress, "balancebuilding_tickProgress", 0);
            Scribe_Values.Look(ref workoutIndex, "balancebuilding_workoutIndex", -1);
            Scribe_Values.Look(ref memoryFactor, "balancebuilding_memoryFactor", 1f);
            Scribe_Values.Look(ref workoutEfficiencyValue, "balancebuilding_workoutEfficiencyValue", 1f);
            Scribe_Values.Look(ref pawnOffset, "balancebuilding_pawnOffset", Vector3.zero);
            Scribe_Values.Look(ref lyingRotation, "balancebuilding_lyingRotation", Rot4.Invalid);
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

            if (workoutIndex < 0) workoutIndex = GetWorkoutInt(compPhysique, ext, RimbodyWorkoutCategory.Balance, out memoryFactor);
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
                if (buildingAnimated != null)
                {
                    buildingAnimated.beingUsed = true;
                }
            };
            float uptime = 0.95f - (0.004f * compPhysique.MuscleMass);
            float cycleDuration = 125f - compPhysique.MuscleMass;
            workout.AddPreTickAction(delegate
            {
                WatchTickAction(buildingAnimated, exWorkout, uptime, cycleDuration);
            });
            workout.handlingFacing = true;
            workout.defaultCompleteMode = ToilCompleteMode.Delay;
            workout.defaultDuration = duration;
            workout.AddFinishAction(delegate
            {
                FinishWorkout(compPhysique);
                if (buildingAnimated != null)
                {
                    buildingAnimated.beingUsed = false;
                    buildingAnimated.calculatedOffset = Vector3.zero;
                    pawnOffset = Vector3.zero;
                }
                pawn.jobs.posture = PawnPosture.Standing;
                pawn.SetPawnBodyAngleOverride(-1f);
                lyingRotation = Rot4.Invalid;
                Rimbody_Utility.TryGainGymThought(pawn);
                Rimbody_Utility.AddMemory(compPhysique, RimbodyWorkoutCategory.Balance, ext.workouts[workoutIndex].name);
            });
            yield return workout;
        }
    }
}
