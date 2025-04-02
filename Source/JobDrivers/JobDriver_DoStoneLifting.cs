using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using Verse.Sound;
using System.Reflection;
using UnityEngine;
using System;
using UnityEngine.Profiling;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoStoneLifting : JobDriver
    {
        private float joygainfactor = 1.0f;
        private int tickProgress = 0;
        private float muscleInt = 25;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, 0, null, errorOnFailed))
            {
                return false;
            }

            return true;
        }

        private void AddMemory(CompPhysique compPhysique)
        {
            if (compPhysique != null)
            {
                compPhysique.lastWorkoutTick = Find.TickManager.TicksGame;
                compPhysique.AddNewMemory($"strength|stonelifting");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref tickProgress, "stonelifting_tickProgress", 0);
            Scribe_Values.Look(ref muscleInt, "stonelifting_muscleInt", 25);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            muscleInt = compPhysique.MuscleMass;
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.AddEndCondition(() => (!compPhysique.resting) ? JobCondition.Ongoing : JobCondition.InterruptForced);
            EndOnTired(this);
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_General.DoAtomic(delegate
            {
                pawn.carryTracker.TryStartCarry(TargetA.Thing, 1);
            });
            //IntVec3 workoutspot = RCellFinder.SpotToStandDuringJob(pawn);
            //yield return Toils_Goto.GotoCell(workoutspot, PathEndMode.OnCell);

            Toil workout;
            workout = ToilMaker.MakeToil("MakeNewToils");
            workout.initAction = () =>
            {
                pawn.pather.StopDead();
                pawn.rotationTracker.FaceCell(pawn.Position + new IntVec3(0, 0, -1));
                var joyneed = pawn.needs?.joy;
                if (joyneed?.tolerances.BoredOf(DefOf_Rimbody.Rimbody_WorkoutJoy) == true)
                {
                    joygainfactor = 0;
                }
            };
            workout.tickAction = delegate
            {
                tickProgress += 1;
                pawn.needs?.joy?.GainJoy(1.0f * joygainfactor * 0.36f / 2500f, DefOf_Rimbody.Rimbody_WorkoutJoy);
            };
            workout.defaultCompleteMode = ToilCompleteMode.Delay;
            workout.defaultDuration = 800;
            workout.AddFinishAction(delegate
            {
                pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
                AddMemory(compPhysique);
            });
            yield return workout;
        }

        public override bool ModifyCarriedThingDrawPos(ref Vector3 drawPos, ref bool flip)
        {
            return ModifyCarriedThingDrawPosWorker(ref drawPos, ref flip, pawn, tickProgress, muscleInt);
        }
        public static bool ModifyCarriedThingDrawPosWorker(ref Vector3 drawPos, ref bool flip, Pawn pawn, int tickProgress, float muscleInt)
        {
            Thing carriedThing = pawn.carryTracker.CarriedThing;
            if (carriedThing == null)
            {
                return false;
            }
            float uptime = 0.95f - (40f * muscleInt / 5000f);
            float cycleDuration = 150f-muscleInt;
            float jiggle_amount = 3f * (1f - (muscleInt / 50f)) / 100f;
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

            float xJitter = (Rand.RangeSeeded(-jiggle_amount, jiggle_amount, tickProgress));
            if (tickProgress>0)
            {
                drawPos += new Vector3(xJitter, 1f / 26f, yOffset);
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
    }
}
