using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using Verse.Sound;
using System.Reflection;
using UnityEngine;
using System;
using UnityEngine.Profiling;
using System.Linq;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoStrengthLifting : JobDriver
    {
        private float joygainfactor = 1.0f;
        private int tickProgress = 0;
        private float muscleInt = 25;
        private WorkOut exWorkout = null;
        private int side = 1;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, 0, null, errorOnFailed))
            {
                return false;
            }

            return true;
        }
        private int GetWorkoutInt(CompPhysique compPhysique, ModExtensionRimbodyTarget ext, out float score)
        {
            score = 0f;
            int indexBest = -1;
            var numVarieties = ext.workouts.Count;
            for (int i = 0; i < numVarieties; i++)
            {
                var tempscore = Math.Max(score, compPhysique.GetScore(RimbodyTargetCategory.Strength, ext.workouts[i]));
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
                compPhysique.AddNewMemory($"strength|{name}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref tickProgress, "strengthlifting_tickProgress", 0);
            Scribe_Values.Look(ref muscleInt, "strengthlifting_muscleInt", 25);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            muscleInt = compPhysique.MuscleMass;
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.AddEndCondition(() => (RimbodySettings.useFatigue && compPhysique.resting) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            EndOnTired(this);
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
            //IntVec3 workoutspot = RCellFinder.SpotToStandDuringJob(pawn);
            IntVec3 workoutspot = SpotToWorkoutStandingNear(pawn, TargetA.Thing);
            yield return Toils_Goto.GotoCell(workoutspot, PathEndMode.OnCell);

            var ext = TargetThingA.def.GetModExtension<ModExtensionRimbodyTarget>();
            var workoutIndex = GetWorkoutInt(compPhysique, ext, out var score);
            exWorkout = ext.workouts[workoutIndex];
            if (exWorkout.reportString != null)
            {
                this.job.reportStringOverride = exWorkout.reportString.Translate();
            }
            Toil workout;
            workout = ToilMaker.MakeToil("MakeNewToils");
            workout.initAction = () =>
            {
                int random = Rand.Range(0, 2);
                side = random == 0 ? -1 : 1;
                pawn.pather.StopDead();
                pawn.rotationTracker.FaceCell(pawn.Position + new IntVec3(0, 0, -1));
                var joyneed = pawn.needs?.joy;
                if (joyneed?.tolerances.BoredOf(DefOf_Rimbody.Rimbody_WorkoutJoy) == true)
                {
                    joygainfactor = 0;
                }
                compPhysique.jobOverride = true;
                compPhysique.limitOverride = score <= exWorkout.strength * 0.9f;
                compPhysique.strengthOverride = score;
                compPhysique.cardioOverride = 0.2f;
            };
            workout.tickAction = delegate
            {
                tickProgress += 1;
                pawn.needs?.joy?.GainJoy(1.0f * joygainfactor * 0.36f / 2500f, DefOf_Rimbody.Rimbody_WorkoutJoy);
            };
            workout.handlingFacing = true;
            workout.defaultCompleteMode = ToilCompleteMode.Delay;
            workout.defaultDuration = 800;
            workout.AddFinishAction(delegate
            {
                //pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
                compPhysique.jobOverride = false;
                compPhysique.limitOverride = false;
                compPhysique.strengthOverride = 0f;
                compPhysique.cardioOverride = 0f;
                TryGainGymThought();
                AddMemory(compPhysique, ext.workouts[workoutIndex].name);
                compPhysique.AddPartFatigue(ext.workouts[workoutIndex].strengthParts);
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
            return ModifyCarriedThingDrawPosWorker(ref drawPos, ref flip, pawn, tickProgress, muscleInt, exWorkout.itemOffset, side);
        }
        public static bool ModifyCarriedThingDrawPosWorker(ref Vector3 drawPos, ref bool flip, Pawn pawn, int tickProgress, float muscleInt, Vector3 offset, int side)
        {
            Thing carriedThing = pawn.carryTracker.CarriedThing;
            if (carriedThing == null)
            {
                return false;
            }
            float uptime = 0.95f - (15f * muscleInt / 5000f);
            float cycleDuration = 125f - muscleInt;
            float jitter_amount = 3f * Mathf.Max(0f,(1f - (muscleInt / 35f))) / 100f;
            float cycleTime = (tickProgress % (int)cycleDuration) / cycleDuration;
            float yOffset = 0f;
            if (cycleTime < uptime)
            {
                yOffset = Mathf.Lerp(0f, 0.3f, cycleTime / uptime);
            }
            else
            {
                yOffset = Mathf.Lerp(0.3f, 0f, (cycleTime - uptime) / (1f - uptime));
            }
            offset.x = offset.x * side;

            float xJitter = (Rand.RangeSeeded(-jitter_amount, jitter_amount, tickProgress));
            if (tickProgress > 0)
            {
                drawPos += new Vector3(xJitter, 1f / 26f, yOffset) + offset;
                flip = false;
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

        public static IntVec3 SpotToWorkoutStandingNear(Pawn pawn, Thing workoutThing, Predicate<IntVec3> extraValidator = null)
        {
            IntVec3 workoutLocation = RCellFinder.RandomWanderDestFor(pawn, workoutThing.Position, 8, (Pawn p, IntVec3 c, IntVec3 root) => (root.GetRoom(p.Map) == null || WanderRoomUtility.IsValidWanderDest(p, c, root)) ? true : false, PawnUtility.ResolveMaxDanger(pawn, Danger.Some));
            if (workoutLocation == null)
            {
                if (CellFinder.TryFindRandomReachableNearbyCell(workoutThing.Position, pawn.Map, 5, TraverseParms.For(pawn), (IntVec3 x) => x.Standable(pawn.Map), (Region x) => true, out workoutLocation))
                {
                    return workoutLocation;
                }
                return IntVec3.Invalid;
                //return RCellFinder.SpotToStandDuringJob(extraValidator: delegate (IntVec3 c)
                //{
                //    if (!TryFindAdjacentWorkoutPlaceSpot(c, workoutThing.def, pawn, out var _))
                //    {
                //        return false;
                //    }
                //    return (extraValidator == null || extraValidator(c)) ? true : false;
                //}, pawn: pawn);
            }
            return workoutLocation;
        }
        //public static bool TryFindAdjacentWorkoutPlaceSpot(IntVec3 root, ThingDef workoutDef, Pawn pawn, out IntVec3 placeSpot)
        //{

        //    List<IntVec3> spotSearchList = new List<IntVec3>();
        //    List<IntVec3> cardinals = GenAdj.CardinalDirections.ToList();
        //    List<IntVec3> diagonals = GenAdj.DiagonalDirections.ToList();
        //    placeSpot = IntVec3.Invalid;
        //    spotSearchList.Clear();
        //    cardinals.Shuffle();
        //    for (int j = 0; j < 4; j++)
        //    {
        //        spotSearchList.Add(cardinals[j]);
        //    }
        //    diagonals.Shuffle();
        //    for (int k = 0; k < 4; k++)
        //    {
        //        spotSearchList.Add(diagonals[k]);
        //    }
        //    spotSearchList.Add(IntVec3.Zero);
        //    for (int l = 0; l < spotSearchList.Count; l++)
        //    {
        //        IntVec3 intVec2 = root + spotSearchList[l];
        //        if (intVec2.Walkable(pawn.Map) && !intVec2.IsForbidden(pawn) && !pawn.Map.thingGrid.ThingsAt(intVec2).Any((Thing t) => t.def == workoutDef))
        //        {
        //            placeSpot = intVec2;
        //            return true;
        //        }
        //    }
        //    return false;
        //}
    }
}
