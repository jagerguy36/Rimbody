using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using Verse.Sound;
using System.Reflection;
using UnityEngine;
using System;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoStoneLifting : JobDriver
    {
        private float joygainfactor = 1.0f;

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

        protected override IEnumerable<Toil> MakeNewToils()
        {
            float curtick = 0;
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.AddEndCondition(() => (!compPhysique.resting) ? JobCondition.Ongoing : JobCondition.InterruptForced);
            EndOnTired(this);
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            
            Toil workout;
            workout = ToilMaker.MakeToil("MakeNewToils");
            workout.initAction = () =>
            {
                pawn.inventory.GetDirectlyHeldThings().TryAddOrTransfer(TargetA.Thing.SplitOff(1));
                pawn.pather.StopDead();
                pawn.rotationTracker.FaceCell(pawn.Position + new IntVec3(0, 0, -1));
                var joyneed = pawn.needs?.joy;
                if (joyneed?.tolerances.BoredOf(DefOf_Rimbody.Rimbody_WorkoutJoy) == true)
                {
                    joygainfactor = 0;
                }
            };
            float oscillationSpeed = 0.05f;
            float oscillationAmplitude = 0.1f;
            workout.tickAction = delegate
            {
                float yOffset = Mathf.Sin(curtick * oscillationSpeed) * oscillationAmplitude;
                pawn.needs?.joy?.GainJoy(1.0f * joygainfactor * 0.36f / 2500f, DefOf_Rimbody.Rimbody_WorkoutJoy);
                TargetA.Thing.DrawNowAt(pawn.DrawPos + new Vector3(0, 1f, yOffset));
                curtick += 1;
            };
            workout.defaultCompleteMode = ToilCompleteMode.Delay;
            workout.defaultDuration = 1000;
            workout.AddFinishAction(delegate
            {
                pawn.inventory.GetDirectlyHeldThings().TryDrop(TargetA.Thing, pawn.Position, pawn.Map, ThingPlaceMode.Near, 1, out _);
                AddMemory(compPhysique);
            });
            yield return workout;
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
