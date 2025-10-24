using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using UnityEngine;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoStrengthLifting : JobDriver_RimbodyBaseDriver
    {
        private const int duration = 800;
        private Vector3 itemOffset = Vector3.zero;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }
            return true;
        }
        protected void WatchTickAction(Thing_WorkoutAnimated item, WorkOut wo, float uptime, float cycleDuration, float jitter_amount)
        {
            tickProgress++;
            if (wo.movingpartAnimOffset?.south != null && wo.movingpartAnimPeak?.south != null)
            {
                float cycleTime = (tickProgress % (int)cycleDuration) / cycleDuration;
                int cycleIndex = (int)(tickProgress / cycleDuration);
                float nudgeMultiplier;
                if (cycleTime < uptime)
                {
                    nudgeMultiplier = Mathf.Lerp(0f, 1f, cycleTime / uptime);
                }
                else
                {
                    nudgeMultiplier = Mathf.Lerp(1f, 0f, (cycleTime - uptime) / (1f - uptime));
                }
                Vector3 woOffset = wo.movingpartAnimOffset.FromRot(pawn.Rotation);
                Vector3 woNudge = wo.movingpartAnimPeak.FromRot(pawn.Rotation);
                float armIndex;

                switch (wo.animationType)
                {
                    case InteractionType.Item:
                        armIndex = (cycleIndex % 2 == 0) ? 1f : -1f;
                        if(pawn.Rotation == Rot4.South || pawn.Rotation == Rot4.North)
                        {
                            woOffset.x *= armIndex;
                            woNudge.x *= armIndex;
                            itemOffset = woOffset + nudgeMultiplier * woNudge;
                            itemOffset.x += Rand.Range(-jitter_amount, jitter_amount);
                        }
                        else
                        {
                            woOffset.z *= armIndex;
                            woNudge.z *= armIndex;
                            itemOffset = woOffset + nudgeMultiplier * woNudge;
                            itemOffset.z += Rand.Range(-jitter_amount, jitter_amount);
                        }
                        break;
                    case InteractionType.ItemEach:
                        armIndex = (cycleIndex % 2 == 0) ? 1f : -1f;
                        if (pawn.Rotation == Rot4.South || pawn.Rotation == Rot4.North)
                        {
                            woOffset.x *= armIndex;
                            woNudge.x *= armIndex;
                            var randValue = Rand.Range(-jitter_amount, jitter_amount);
                            itemOffset = woOffset + nudgeMultiplier * woNudge;
                            item.ghostOffset.x = -itemOffset.x - woOffset.x;
                            item.ghostOffset.z = -itemOffset.z + woOffset.z;
                            itemOffset.x += randValue;
                            item.ghostOffset.x -= randValue;
                        }
                        else
                        {
                            woOffset.z *= armIndex;
                            var randValue = Rand.Range(-jitter_amount, jitter_amount);
                            itemOffset = woOffset + nudgeMultiplier * woNudge;
                            item.ghostOffset.z = -itemOffset.z - woOffset.z;
                            item.ghostOffset.x = -itemOffset.x + woOffset.x;
                            itemOffset.x += randValue;
                            item.ghostOffset.x -= randValue;
                            //item.ghostOffset.y -= armIndex * 0.03474903f;
                            item.ghostOffset.y = armIndex * -0.03474903f;
                        }
                        break;
                    case InteractionType.ItemBoth:
                        if (pawn.Rotation == Rot4.South || pawn.Rotation == Rot4.North)
                        {
                            itemOffset = woOffset + nudgeMultiplier * woNudge;
                            item.ghostOffset.x = -itemOffset.x -woOffset.x - nudgeMultiplier * woNudge.x;
                            item.ghostOffset.x += Rand.Range(-jitter_amount, jitter_amount);
                            itemOffset.x += Rand.Range(-jitter_amount, jitter_amount);
                        }
                        else
                        {
                            itemOffset = woOffset + nudgeMultiplier * woNudge;
                            item.ghostOffset.z = 0.45f;
                            item.ghostOffset.y = -0.03474903f;
                            item.ghostOffset.z += Rand.Range(-jitter_amount, jitter_amount);
                            itemOffset.x += Rand.Range(-jitter_amount, jitter_amount);
                        }
                        break;
                    default:
                        break;
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
            Scribe_Values.Look(ref joygainfactor, "strengthlifting_joygainfactor", 1.0f);
            Scribe_Values.Look(ref tickProgress, "strengthlifting_tickProgress", 0);
            Scribe_Values.Look(ref workoutIndex, "strengthlifting_workoutIndex", -1);
            Scribe_Values.Look(ref memoryFactor, "strengthlifting_memoryFactor", 1f);
            Scribe_Values.Look(ref workoutEfficiencyValue, "strengthlifting_workoutEfficiencyValue", 1.0f);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var compPhysique = pawn.compPhysique();
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.AddEndCondition(() => (RimbodySettings.useExhaustion && compPhysique.resting) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            this.AddEndCondition(() => (compPhysique.gain >= compPhysique.gainMax) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            Rimbody_Utility.EndOnTired(this);

            //Set up workout
            RimbodyDefLists.ThingModExDB.TryGetValue(TargetThingA.def.shortHash, out var ext);
            Thing_WorkoutAnimated thingAnimated = (Thing_WorkoutAnimated)job.GetTarget(TargetIndex.A).Thing;
            if (workoutIndex < 0)
            {
                workoutIndex = GetWorkoutInt(compPhysique, ext, RimbodyWorkoutCategory.Strength, out memoryFactor);
            }
            var exWorkout = ext.workouts[workoutIndex];

            yield return Toils_General.DoAtomic(delegate
            {
                job.count = 1;
            });
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A).FailOnDestroyedNullOrForbidden(TargetIndex.A);
            yield return Toils_Rimbody.GotoSpotToWorkout(TargetIndex.B, exWorkout.spot);
            Toil workout;
            workout = ToilMaker.MakeToil("MakeNewToils");
            workout.initAction = () =>
            {
                pawn.pather.StopDead();
                if (TargetB.Thing != null)
                {
                    if (exWorkout.pawnDirection == Direction.LyingFrontSame)
                    {
                        pawn.SetPawnBodyAngleOverride(TargetB.Thing.Rotation.Opposite.AsAngle);
                        pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
                    }
                    pawn.Rotation = TargetB.Thing.Rotation;
                    thingAnimated.drawRotation = TargetB.Thing.Rotation;
                    if (TargetB.Thing?.def == DefOf_Rimbody.Rimbody_FlatBench)
                    {
                        pawnOffset.z = 0.5f;
                    }    
                    workoutEfficiencyValue = 1.05f;
                }
                else
                {
                    pawn.Rotation = Rot4.South;
                    thingAnimated.drawRotation = Rot4.South;
                }
                if (exWorkout.reportString != null)
                {
                    job.reportStringOverride = exWorkout.reportString.Translate();
                }
                AdjustJoygainFactor();
                StartWorkout(compPhysique, exWorkout);
                thingAnimated.beingUsed = true;
            };
            float uptime = 0.95f - (0.004f * compPhysique.MuscleMass);
            float cycleDuration = 125f - compPhysique.MuscleMass;
            float jitter_amount = 0.03f * Mathf.Max(0f, (1f - (compPhysique.MuscleMass / 35f)));
            workout.tickAction = delegate
            {
                WatchTickAction(thingAnimated, exWorkout, uptime, cycleDuration, jitter_amount);
            };
            workout.handlingFacing = true;
            workout.defaultCompleteMode = ToilCompleteMode.Delay;
            workout.defaultDuration = duration;
            workout.AddFinishAction(delegate
            {
                FinishWorkout(compPhysique);
                thingAnimated.beingUsed = false;
                thingAnimated.ghostOffset = Vector3.zero;
                pawn.SetPawnBodyAngleOverride(-1f);
                Rimbody_Utility.TryGainGymThought(pawn);
                Rimbody_Utility.AddMemory(compPhysique, RimbodyWorkoutCategory.Strength, exWorkout.name);
                Job haulJob = new WorkGiver_HaulGeneral().JobOnThing(pawn, pawn.carryTracker.CarriedThing);
                if (haulJob?.TryMakePreToilReservations(pawn, true) ?? false)
                {
                    pawn.jobs.jobQueue.EnqueueFirst(haulJob);
                }
            });
            yield return workout;
        }
        public override bool ModifyCarriedThingDrawPos(ref Vector3 drawPos, ref bool flip)
        {
            if (tickProgress > 0)
            {
                drawPos += itemOffset;
                return true;
            }
            return false;
        }
    }
}
