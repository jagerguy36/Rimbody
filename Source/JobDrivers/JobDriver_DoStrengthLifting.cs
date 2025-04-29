using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using UnityEngine;
using System;
using Verse.Sound;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoStrengthLifting : JobDriver
    {
        private const int duration = 800;
        private float joygainfactor = 1.0f;
        private int tickProgress = 0;
        private Vector3 itemOffset = Vector3.zero;

        private readonly ThingDef benchDef = DefDatabase<ThingDef>.GetNamed("Rimbody_FlatBench");
        private ModExtensionRimbodyTarget ext => TargetThingA.def.GetModExtension<ModExtensionRimbodyTarget>();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, 0, null, errorOnFailed))
            {
                return false;
            }
            return true;
        }
        protected void WatchTickAction(Thing_WorkoutAnimated item, WorkOut wo, float actorMuscle)
        {
            tickProgress++;
            if (tickProgress > 0)
            {
                if (wo.movingpartAnimOffset?.south != null && wo.movingpartAnimPeak?.south != null)
                {
                    float uptime = 0.95f - (20f * actorMuscle / 5000f);
                    float cycleDuration = 125f - actorMuscle;
                    float jitter_amount = 3f * Mathf.Max(0f, (1f - (actorMuscle / 35f))) / 100f;
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
                    Vector3 woOffset = wo.movingpartAnimOffset.south;
                    Vector3 woNudge = wo.movingpartAnimPeak.south;
                    int armIndex;
                    float xJitter;
                    Vector3 jitterVector;

                    switch (wo.animationType)
                    {
                        case InteractionType.item:
                            armIndex = (cycleIndex % 2 == 0) ? 1 : -1;
                            woOffset.x *= armIndex;
                            woNudge.x *= armIndex;
                            itemOffset = woOffset + nudgeMultiplier * woNudge;
                            itemOffset.x += Rand.Range(-jitter_amount, jitter_amount);
                            break;
                        case InteractionType.itemEach:
                            //int direction = (cycleIndex % 2 == 0) ? 1 : -1;
                            //woOffset.x *= direction;
                            //woNudge.x *= direction;
                            //float xJitter = (Rand.RangeSeeded(-jitter_amount, jitter_amount, tickProgress));
                            //Vector3 jitterVector = new Vector3(xJitter, 1f / 26f, 0);
                            //itemOffset = woOffset + nudgeMultiplier * woNudge + jitterVector;
                            break;
                        case InteractionType.itemBoth:
                            itemOffset = woOffset + nudgeMultiplier * woNudge;
                            item.ghostOffset.x = -itemOffset.x * 2 + Rand.Range(-jitter_amount, jitter_amount);
                            itemOffset.x += Rand.Range(-jitter_amount, jitter_amount);
                            break;
                        default:
                            break;
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
            Scribe_Values.Look(ref tickProgress, "strengthlifting_tickProgress", 0);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.AddEndCondition(() => (RimbodySettings.useExhaustion && compPhysique.resting) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            this.AddEndCondition(() => (compPhysique.gain >= compPhysique.gainMax * 0.95f) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            EndOnTired(this);
            var workoutIndex = GetWorkoutInt(compPhysique, ext, out var score);
            var exWorkout = ext.workouts[workoutIndex];
            Thing_WorkoutAnimated thingAnimated = (Thing_WorkoutAnimated)job.GetTarget(TargetIndex.A).Thing;
            yield return Toils_General.DoAtomic(delegate
            {
                job.count = 1;
            });
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_General.DoAtomic(delegate
            {
                pawn.carryTracker.TryStartCarry(TargetA.Thing, 1);
            });
            IntVec3 workoutspot = SpotToWorkout(pawn, TargetA.Thing, out bool usingBench, exWorkout.useBench);
            float efficiency = 1f;
            if (usingBench)
            {
                efficiency = 1.05f;
            }
            this.job.SetTarget(TargetIndex.C, workoutspot);
            yield return Toils_Reserve.Reserve(TargetIndex.C);
            yield return Toils_Goto.GotoCell(workoutspot, PathEndMode.OnCell);
            if (exWorkout.reportString != null)
            {
                this.job.reportStringOverride = exWorkout.reportString.Translate();
            }
            Toil workout;
            workout = ToilMaker.MakeToil("MakeNewToils");
            workout.initAction = () =>
            {
                Log.Message("initiated");
                int random = Rand.Range(0, 2);
                var side = random == 0 ? -1 : 1;
                pawn.pather.StopDead();
                pawn.rotationTracker.FaceCell(pawn.Position + new IntVec3(0, 0, -1));
                var joyneed = pawn.needs?.joy;
                if (joyneed?.tolerances.BoredOf(DefOf_Rimbody.Rimbody_WorkoutJoy) == true)
                {
                    joygainfactor = 0;
                }
                thingAnimated.beingUsed = true;
                compPhysique.jobOverride = true;
                compPhysique.strengthOverride = exWorkout.strength * efficiency;
                compPhysique.cardioOverride = exWorkout.cardio * efficiency;
                compPhysique.partsOverride = exWorkout.strengthParts;
            };
            workout.tickAction = delegate
            {
                WatchTickAction(thingAnimated, exWorkout, compPhysique.MuscleMass);
            };
            workout.handlingFacing = true;
            workout.defaultCompleteMode = ToilCompleteMode.Delay;
            workout.defaultDuration = duration;
            workout.AddFinishAction(delegate
            {
                thingAnimated.beingUsed = false;
                thingAnimated.ghostOffset = Vector3.zero;
                compPhysique.jobOverride = false;
                compPhysique.strengthOverride = 0f;
                compPhysique.cardioOverride = 0f;
                compPhysique.partsOverride = null;
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

        public IntVec3 SpotToWorkout(Pawn pawn, Thing workoutThing, out bool usingBench, bool lookForBench = false)
        {
            usingBench = false;
            IntVec3 workoutLocation = IntVec3.Invalid;
            if (lookForBench)
            {
                Thing thing = null;
                Predicate<Thing> baseChairValidator = delegate (Thing t)
                {
                    if (t.def.building == null)
                    {
                        return false;
                    }
                    if (t.IsForbidden(pawn))
                    {
                        return false;
                    }
                    if (!t.IsSociallyProper(pawn))
                    {
                        return false;
                    }
                    if (t.IsBurning())
                    {
                        return false;
                    }
                    if (!TryFindFreeSittingSpotOnThing(t, pawn, out var cell))
                    {
                        return false;
                    }
                    if (!pawn.CanReserve(cell))
                    {
                        return false;
                    }
                    return true;
                };
                thing = GenClosest.ClosestThingReachable(workoutThing.Position, pawn.Map, ThingRequest.ForDef(benchDef), PathEndMode.OnCell, TraverseParms.For(pawn), 30f, (Thing t) => baseChairValidator(t) && t.Position.GetDangerFor(pawn, t.Map) == Danger.None);
                if (thing != null && TryFindFreeSittingSpotOnThing(thing, pawn, out workoutLocation))
                {
                    usingBench = true;
                    return workoutLocation;
                }
            }
            workoutLocation = RCellFinder.RandomWanderDestFor(pawn, workoutThing.Position, 8, (Pawn p, IntVec3 c, IntVec3 root) => (root.GetRoom(p.Map) == null || WanderRoomUtility.IsValidWanderDest(p, c, root)) ? true : false, PawnUtility.ResolveMaxDanger(pawn, Danger.Some));
            if (workoutLocation == IntVec3.Invalid)
            {
                if (CellFinder.TryFindRandomReachableNearbyCell(workoutThing.Position, pawn.Map, 5, TraverseParms.For(pawn), (IntVec3 x) => x.Standable(pawn.Map), (Region x) => true, out workoutLocation))
                {
                    return workoutLocation;
                }
                return IntVec3.Invalid;
            }
            return workoutLocation;
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
