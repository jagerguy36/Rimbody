using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoStrengthBuilding : JobDriver_RimbodyBaseDriver
    {
        private const int duration = 1000;
        private WorkoutTickHandler externalHandler = null;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed)) return false;
            if (!pawn.ReserveSittableOrSpot(job.targetB.Cell, job, errorOnFailed)) return false;
            return true;
        }

        protected void WatchTickAction(Building_WorkoutAnimated building, WorkOut wo, float uptime, float cycleDuration, float jitter_amount)
        {
            tickProgress++;
            if (externalHandler != null)
            {
                externalHandler.TickAction(pawn, building, wo, uptime, cycleDuration, jitter_amount, tickProgress, ref pawnOffset, ref lyingRotation);
            }
            else
            {
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
                        if (building != null)
                        {
                            pawn.Drawer.Notify_MeleeAttackOn(building);
                        }
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
            Scribe_Values.Look(ref joygainfactor, "strengthbuilding_joygainfactor", 1.0f);
            Scribe_Values.Look(ref tickProgress, "strengthbuilding_tickProgress", 0);
            Scribe_Values.Look(ref workoutIndex, "strengthbuilding_workoutIndex", -1);
            Scribe_Values.Look(ref memoryFactor, "strengthbuilding_memoryFactor", 1f);
            Scribe_Values.Look(ref workoutEfficiencyValue, "strengthbuilding_workoutEfficiencyValue", 1f);
            Scribe_Values.Look(ref pawnOffset, "strengthbuilding_pawnOffset", Vector3.zero);
            Scribe_Values.Look(ref lyingRotation, "strengthbuilding_lyingRotation", Rot4.Invalid);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var compPhysique = pawn.compPhysique();
            this.EndOnDespawnedOrNull(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.AddEndCondition(() => (RimbodySettings.useExhaustion && compPhysique.resting) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            this.AddEndCondition(() => (compPhysique.gain >= compPhysique.gainMax) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            Rimbody_Utility.EndOnTired(this);
            RimbodyDefLists.ThingModExDB.TryGetValue(TargetThingA.def.shortHash, out var ext);
            Building_WorkoutAnimated buildingAnimated = TargetThingA as Building_WorkoutAnimated;

            if (workoutIndex < 0) workoutIndex = GetWorkoutInt(compPhysique, ext, RimbodyWorkoutCategory.Strength, out memoryFactor);
            var exWorkout = ext.workouts[workoutIndex];
            workoutEfficiencyValue = TargetThingA.GetStatValue(DefOf_Rimbody.Rimbody_WorkoutEfficiency);

            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);

            if (exWorkout.reportString != null)
            {
                this.job.reportStringOverride = exWorkout.reportString.Translate();
            }
            if (exWorkout.customWorkoutTickHandler != null)
            {
                externalHandler = exWorkout.customWorkoutTickHandler;
            }
            Toil workout;
            workout = ToilMaker.MakeToil("MakeNewToils");
            workout.initAction = () =>
            {
                TargetThingA.Map.physicalInteractionReservationManager.Reserve(pawn, job, TargetThingA);
                pawn.pather.StopDead();
                GetInPosition(TargetThingA, exWorkout.pawnDirection);
                joygainfactor = TargetThingA.def.GetStatValueAbstract(StatDefOf.JoyGainFactor);
                AdjustJoygainFactor();
                StartWorkout(compPhysique, exWorkout);
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
                Rimbody_Utility.AddMemory(compPhysique, RimbodyWorkoutCategory.Strength, ext.workouts[workoutIndex].name);
            });
            yield return workout;
        }
    }
}
