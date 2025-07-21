using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using UnityEngine;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoBalanceLifting : JobDriver
    {
        private const int duration = 1500;
        private float joygainfactor = 1.0f;
        private int tickProgress = 0;
        private int workoutIndex = -1;
        private float memoryFactor = 1.0f;
        private Vector3 pawnOffset = Vector3.zero;
        private Vector3 itemOffset = Vector3.zero;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed))
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
        protected void WatchTickAction(Thing_WorkoutAnimated item, WorkOut wo, float uptime, float cycleDuration)
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
                        }
                        else
                        {
                            woOffset.z *= armIndex;
                            woNudge.z *= armIndex;
                            itemOffset = woOffset + nudgeMultiplier * woNudge;
                        }
                        break;
                    case InteractionType.ItemEach:
                        armIndex = (cycleIndex % 2 == 0) ? 1f : -1f;
                        if (pawn.Rotation == Rot4.South || pawn.Rotation == Rot4.North)
                        {
                            woOffset.x *= armIndex;
                            woNudge.x *= armIndex;
                            itemOffset = woOffset + nudgeMultiplier * woNudge;
                            item.ghostOffset.x = -itemOffset.x - woOffset.x;
                            item.ghostOffset.z = -itemOffset.z + woOffset.z;
                        }
                        else
                        {
                            woOffset.z *= armIndex;
                            itemOffset = woOffset + nudgeMultiplier * woNudge;
                            item.ghostOffset.z = -itemOffset.z - woOffset.z;
                            item.ghostOffset.x = -itemOffset.x + woOffset.x;
                            //item.ghostOffset.y -= armIndex * 0.03474903f;
                            item.ghostOffset.y = armIndex * -0.03474903f;
                        }
                        break;
                    case InteractionType.ItemBoth:
                        if (pawn.Rotation == Rot4.South || pawn.Rotation == Rot4.North)
                        {
                            itemOffset = woOffset + nudgeMultiplier * woNudge;
                            item.ghostOffset.x = -itemOffset.x -woOffset.x - nudgeMultiplier * woNudge.x;
                        }
                        else
                        {
                            itemOffset = woOffset + nudgeMultiplier * woNudge;
                            item.ghostOffset.z = 0.45f;
                            item.ghostOffset.y = -0.03474903f;
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
        private int GetWorkoutInt(CompPhysique compPhysique, ModExtensionRimbodyTarget ext, out float memoryFactor)
        {
            float score = 0f;
            memoryFactor = 1f;
            int indexBest = -1;
            var numVarieties = ext.workouts.Count;
            if (numVarieties == 1)
            {
                memoryFactor = compPhysique.memory.Contains("balance|" + ext.workouts[0].name) ? 0.9f : 1f;
                return 0;
            }
            for (int i = 0; i < numVarieties; i++)
            {
                if (ext.workouts[i].Category != RimbodyWorkoutCategory.Balance)
                {
                    continue;
                }
                float tmpMemoryFactor = compPhysique.memory.Contains("balance|" + ext.workouts[i].name) ? 0.9f : 1f;
                float tmpScore = tmpMemoryFactor * compPhysique.GetWorkoutScore(RimbodyWorkoutCategory.Balance, ext.workouts[i]);
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
        private void AddMemory(CompPhysique compPhysique, string name)
        {
            if (compPhysique != null)
            {
                compPhysique.lastWorkoutTick = Find.TickManager.TicksGame;
                compPhysique.AddNewMemory($"balance|{name}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref joygainfactor, "balancelifting_joygainfactor", 1.0f);
            Scribe_Values.Look(ref tickProgress, "balancelifting_tickProgress", 0);
            Scribe_Values.Look(ref workoutIndex, "balancelifting_workoutIndex", -1);
            Scribe_Values.Look(ref memoryFactor, "balancelifting_memoryFactor", 1f);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var compPhysique = pawn.compPhysique();
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.AddEndCondition(() => (RimbodySettings.useExhaustion && compPhysique.resting) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            this.AddEndCondition(() => (compPhysique.gain >= compPhysique.gainMax) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            EndOnTired(this);

            //Set up workout
            RimbodyDefLists.BalanceTarget.TryGetValue(TargetThingA.def, out var ext);
            Thing_WorkoutAnimated thingAnimated = (Thing_WorkoutAnimated)job.GetTarget(TargetIndex.A).Thing;
            if (workoutIndex < 0)
            {
                workoutIndex = GetWorkoutInt(compPhysique, ext, out memoryFactor);
            }
            var exWorkout = ext.workouts[workoutIndex];
            float workoutEfficiencyValue = 1f;

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
                thingAnimated.beingUsed = true;
            };
            float uptime = 0.75f - (0.0001f * compPhysique.MuscleMass);
            float cycleDuration = 125f - compPhysique.MuscleMass;
            workout.tickAction = delegate
            {
                WatchTickAction(thingAnimated, exWorkout, uptime, cycleDuration);
            };
            workout.handlingFacing = true;
            workout.defaultCompleteMode = ToilCompleteMode.Delay;
            workout.defaultDuration = duration;
            workout.AddFinishAction(delegate
            {
                compPhysique.jobOverride = false;
                compPhysique.strengthOverride = 0f;
                compPhysique.cardioOverride = 0f;
                compPhysique.partsOverride = null;
                compPhysique.AssignedTick = Mathf.Max(0, compPhysique.AssignedTick - tickProgress);
                thingAnimated.beingUsed = false;
                thingAnimated.ghostOffset = Vector3.zero;
                pawn.SetPawnBodyAngleOverride(-1f);
                TryGainGymThought();
                AddMemory(compPhysique, exWorkout.name);
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
