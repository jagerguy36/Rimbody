using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using UnityEngine;
using System;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoStrengthLifting : JobDriver
    {
        private const int duration = 800;
        private float joygainfactor = 1.0f;
        private int tickProgress = 0;
        private int workoutIndex = -1;
        private float memoryFactor = 1.0f;
        private Vector3 pawnOffset = Vector3.zero;
        private Vector3 itemOffset = Vector3.zero;
        private static readonly ThingDef benchDef = DefDatabase<ThingDef>.GetNamed("Rimbody_FlatBench");

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
                    case InteractionType.item:
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
                    case InteractionType.itemEach:
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
                    case InteractionType.itemBoth:
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
                float tmpMemoryFactor = compPhysique.memory.Contains("strength|" + ext.workouts[i].name) ? 0.9f : 1f;
                float tmpScore = tmpMemoryFactor * compPhysique.GetWorkoutScore(RimbodyTargetCategory.Strength, ext.workouts[i]);
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
                compPhysique.AddNewMemory($"strength|{name}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref joygainfactor, "strengthlifting_joygainfactor", 1.0f);
            Scribe_Values.Look(ref tickProgress, "strengthlifting_tickProgress", 0);
            Scribe_Values.Look(ref workoutIndex, "strengthlifting_workoutIndex", -1);
            Scribe_Values.Look(ref memoryFactor, "strengthlifting_memoryFactor", 1f);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.AddEndCondition(() => (RimbodySettings.useExhaustion && compPhysique.resting) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            this.AddEndCondition(() => (compPhysique.gain >= compPhysique.gainMax * RimbodySettings.gainMaxGracePeriod) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            EndOnTired(this);

            //Set up workout
            RimbodyDefLists.StrengthTarget.TryGetValue(TargetThingA.def, out var ext);
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
            yield return GotoSpotToWorkout(TargetIndex.B, exWorkout.useBench);
            Toil workout;
            workout = ToilMaker.MakeToil("MakeNewToils");
            workout.initAction = () =>
            {
                pawn.pather.StopDead();
                if(TargetB.Thing != null)
                {
                    pawn.Rotation = TargetB.Thing.Rotation;
                    thingAnimated.drawRotation = TargetB.Thing.Rotation;
                    pawnOffset.z = 0.5f;
                    workoutEfficiencyValue = 1.05f;
                }
                else
                {
                    pawn.Rotation =Rot4.South;
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
                compPhysique.jobOverride = false;
                compPhysique.strengthOverride = 0f;
                compPhysique.cardioOverride = 0f;
                compPhysique.partsOverride = null;
                thingAnimated.beingUsed = false;
                thingAnimated.ghostOffset = Vector3.zero;
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
        public static Toil GotoSpotToWorkout(TargetIndex foundBench, bool lookForBench = false)
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.CurJob;
                IntVec3 workoutLocation = IntVec3.Invalid;
                if (lookForBench)
                {
                    Thing thing = null;
                    Predicate<Thing> baseChairValidator = delegate (Thing t)
                    {
                        if (t.def.building == null) return false;
                        if (t.IsForbidden(actor)) return false;
                        if (!t.IsSociallyProper(actor)) return false;
                        if (t.IsBurning()) return false;
                        if (!TryFindFreeSittingSpotOnThing(t, actor, out var cell)) return false;
                        if (!actor.CanReserve(cell)) return false;
                        return true;
                    };
                    thing = GenClosest.ClosestThingReachable(actor.Position, actor.Map, ThingRequest.ForDef(benchDef), PathEndMode.OnCell, TraverseParms.For(actor), 30f, (Thing t) => baseChairValidator(t) && t.Position.GetDangerFor(actor, t.Map) == Danger.None);
                    if (thing != null && TryFindFreeSittingSpotOnThing(thing, actor, out workoutLocation))
                    {
                        curJob.SetTarget(foundBench, thing);
                        actor.Reserve(workoutLocation, actor.CurJob);
                        actor.Map.pawnDestinationReservationManager.Reserve(actor, actor.CurJob, workoutLocation);
                        actor.pather.StartPath(workoutLocation, PathEndMode.OnCell);
                        return;
                    }
                }
                workoutLocation = RCellFinder.SpotToStandDuringJob(extraValidator: delegate (IntVec3 c)
                {
                    if (!actor.CanReserve(c)) return false;
                    if (!c.Standable(actor.Map)) return false;
                    if (c.GetRegion(actor.Map).type == RegionType.Portal) return false;
                    return true;
                }, pawn: actor);
                //if (workoutLocation == IntVec3.Invalid)
                //{
                //    CellFinder.TryFindRandomReachableNearbyCell(actor.Position, actor.Map, 20, TraverseParms.For(actor), (IntVec3 x) => x.Standable(actor.Map) && actor.CanReserveAndReach(x, PathEndMode.OnCell, Danger.Deadly), (Region x) => true, out workoutLocation)
                //}
                if (workoutLocation == IntVec3.Invalid)
                {
                    actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
                }
                actor.Reserve(workoutLocation, actor.CurJob);
                actor.Map.pawnDestinationReservationManager.Reserve(actor, actor.CurJob, workoutLocation);
                actor.pather.StartPath(workoutLocation, PathEndMode.OnCell);
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            return toil;
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
        public static bool TryFindFreeSittingSpotOnThing(Thing t, Pawn pawn, out IntVec3 cell)
        {
            foreach (IntVec3 item in t.OccupiedRect())
            {
                if (pawn.CanReserve(item, 1, -1, null, false)) //(pawn.CanReserveSittableOrSpot(item))
                {
                    cell = item;
                    return true;
                }
            }
            cell = default;
            return false;
        }
    }
}
