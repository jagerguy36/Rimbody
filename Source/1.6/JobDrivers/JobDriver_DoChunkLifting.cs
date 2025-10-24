using System.Collections.Generic;
using Verse.AI;
using Verse;
using UnityEngine;
using RimWorld;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoChunkLifting : JobDriver_RimbodyBaseDriver
    {
        private bool shouldReturn = false;
        private const int duration = 800;
        private Vector3 itemOffset = new Vector3(0f, 1f / 26f, 0f);

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed)) return false;
            return true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref shouldReturn, "chunklifting_shouldReturn", false);
            Scribe_Values.Look(ref joygainfactor, "chunklifting_joygainfactor", 1.0f);
            Scribe_Values.Look(ref tickProgress, "chunklifting_tickProgress", 0);
            Scribe_Values.Look(ref memoryFactor, "chunklifting_memoryFactor", 1.0f);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var compPhysique = pawn.compPhysique();
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.AddEndCondition(() => (RimbodySettings.useExhaustion && compPhysique.resting) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            this.AddEndCondition(() => (compPhysique.gain >= compPhysique.gainMax) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            Rimbody_Utility.EndOnTired(this);

            //Set up workout
            RimbodyDefLists.JobModExDB.TryGetValue(job.def.shortHash, out var exWorkout);
            memoryFactor = compPhysique.memory.Contains("strength|" + job.def.defName) ? 0.9f : 1f;
            yield return Toils_General.DoAtomic(delegate
            {
                shouldReturn = TargetThingA.IsInValidStorage();
                job.count = 1;
            });
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A).FailOnDestroyedNullOrForbidden(TargetIndex.A);
            yield return Toils_Rimbody.GotoSpotToWorkout(TargetIndex.B, ItemSpot.None);

            Toil workout;
            workout = ToilMaker.MakeToil("MakeNewToils");
            workout.initAction = () =>
            {
                pawn.pather.StopDead();
                pawn.rotationTracker.FaceCell(pawn.Position + new IntVec3(0, 0, -1));
                AdjustJoygainFactor();
                StartWorkoutJob(compPhysique, exWorkout);
            };
            float uptime = 0.95f - (0.008f * compPhysique.MuscleMass);
            float cycleDuration = 150f - compPhysique.MuscleMass;
            float jiggle_amount = 0.03f * (1f - (compPhysique.MuscleMass / 50f));
            float nudgeMultiplier;
            workout.tickAction = delegate
            {
                tickProgress++;
                float cycleTime = (tickProgress % (int)cycleDuration) / cycleDuration;
                if (cycleTime < uptime)
                {
                    nudgeMultiplier = Mathf.Lerp(0.3f, 0f, cycleTime / uptime);
                }
                else
                {
                    nudgeMultiplier = Mathf.Lerp(0f, 0.3f, (cycleTime - uptime) / (1f - uptime));
                }
                itemOffset.x = Rand.Range(-jiggle_amount, jiggle_amount);
                itemOffset.z = nudgeMultiplier;
                pawn.needs?.joy?.GainJoy(1.0f * joygainfactor * 0.36f / 2500f, DefOf_Rimbody.Rimbody_WorkoutJoy);
            };
            workout.handlingFacing = true;
            workout.defaultCompleteMode = ToilCompleteMode.Delay;
            workout.defaultDuration = duration;
            workout.AddFinishAction(delegate
            {
                FinishWorkout(compPhysique);
                Rimbody_Utility.AddMemory(compPhysique, RimbodyWorkoutCategory.Strength, job.def.defName);
                if (shouldReturn)
                {
                    Job haulJob = new WorkGiver_HaulGeneral().JobOnThing(pawn, pawn.carryTracker.CarriedThing);
                    if (haulJob?.TryMakePreToilReservations(pawn, true) ?? false) pawn.jobs.jobQueue.EnqueueFirst(haulJob);
                    else pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
                }
                else
                {
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
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
