using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using Verse.Sound;
using System.Reflection;
using UnityEngine;
using System;
using UnityEngine.Profiling;
using static System.Net.Mime.MediaTypeNames;

namespace Maux36.Rimbody
{
    internal class JobDriver_DoChunkSquats : JobDriver
    {
        private const int duration = 800;
        private float joygainfactor = 1.0f;
        private int tickProgress = 0;
        private float muscleInt = 25;
        private Vector3 pawnNudge = Vector3.zero;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, 0, null, errorOnFailed))
            {
                return false;
            }

            return true;
        }
        public override Vector3 ForcedBodyOffset
        {
            get
            {
                return pawnNudge;
            }
        }

        private void AddMemory(CompPhysique compPhysique)
        {
            if (compPhysique != null)
            {
                compPhysique.lastWorkoutTick = Find.TickManager.TicksGame;
                compPhysique.AddNewMemory($"strength|chunk squats");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref tickProgress, "chunksquats_tickProgress", 0);
            Scribe_Values.Look(ref muscleInt, "chunksquats_muscleInt", 25);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var compPhysique = pawn.TryGetComp<CompPhysique>();
            muscleInt = compPhysique.MuscleMass;
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.AddEndCondition(() => (RimbodySettings.useExhaustion && compPhysique.resting) ? JobCondition.InterruptForced : JobCondition.Ongoing);
            EndOnTired(this);
            yield return Toils_General.DoAtomic(delegate
            {
                job.count = 1;
            });
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_General.DoAtomic(delegate
            {
                pawn.carryTracker.TryStartCarry(TargetA.Thing, 1);
            });

            var exWorkout = this.job.def.GetModExtension<ModExtensionRimbodyJob>();
            float score = compPhysique.GetStrengthPartScore(exWorkout.strengthParts, exWorkout.strength, out float fatigueFactor);

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
                compPhysique.jobOverride = true;
                compPhysique.limitOverride = score <= exWorkout.strength * 0.9f;
                compPhysique.strengthOverride = score;
                compPhysique.cardioOverride = exWorkout.cardio;
                compPhysique.durationOverride = duration;
                compPhysique.partsOverride = exWorkout.strengthParts;
            };
            float uptime = 0.95f - (20f * muscleInt / 5000f);
            float cycleDuration = 125f - muscleInt;
            float jiggle_amount = 3f * (1f - (muscleInt / 50f)) / 100f;
            float nudgeMultiplier;
            workout.tickAction = delegate
            {
                tickProgress += 1;
                pawn.needs?.joy?.GainJoy(1.0f * joygainfactor * 0.36f / 2500f, DefOf_Rimbody.Rimbody_WorkoutJoy);
                if (tickProgress > 0)
                {
                    float cycleTime = (tickProgress % (int)cycleDuration) / cycleDuration;
                    if (cycleTime < uptime)
                    {
                        nudgeMultiplier = Mathf.Lerp(0f, 1f, cycleTime / uptime);
                    }
                    else
                    {
                        nudgeMultiplier = Mathf.Lerp(1f, 0f, (cycleTime - uptime) / (1f - uptime));
                    }
                    if (tickProgress > 0)
                    {
                        pawnNudge = new Vector3(Rand.RangeSeeded(-jiggle_amount, jiggle_amount, tickProgress), 0f, nudgeMultiplier * 0.13f - 0.1f);
                    }
                }
            };
            workout.handlingFacing = true;
            workout.defaultCompleteMode = ToilCompleteMode.Delay;
            workout.defaultDuration = duration;
            workout.AddFinishAction(delegate
            {
                compPhysique.jobOverride = false;
                compPhysique.limitOverride = false;
                compPhysique.strengthOverride = 0f;
                compPhysique.cardioOverride = 0f;
                compPhysique.durationOverride = 0;
                compPhysique.partsOverride = null;
                AddMemory(compPhysique);
                pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
            });
            yield return workout;
        }

        public override bool ModifyCarriedThingDrawPos(ref Vector3 drawPos, ref bool flip)
        {
            return ModifyCarriedThingDrawPosWorker(ref drawPos, ref flip, pawn, tickProgress);
        }
        public static bool ModifyCarriedThingDrawPosWorker(ref Vector3 drawPos, ref bool flip, Pawn pawn, int tickProgress)
        {
            Thing carriedThing = pawn.carryTracker.CarriedThing;
            if (carriedThing == null)
            {
                return false;
            }
            if (tickProgress>0)
            {
                drawPos += new Vector3(0f, -1f / 26f, 0.45f);
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
